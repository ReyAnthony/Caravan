using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CaravanSerialization.Attributes;
using CaravanSerialization.Serialization;
using CaravanSerialization.Substitutes;
using CaravanSerialization.Migrations;
using CaravanSerialization.ObjectModel;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaravanSerialization
{
    public interface ICaravan
    {
        public event Action OnAllSaved;
        public event Action OnAllLoaded;
        public event Action<ScriptableObject> BeforeSave;

        public void SaveAll();
        public void LoadAll();
        bool HasSave();
        
        
        public void GenerateAllMissingInstanceID();
        public void CheckDuplicatedIDs(Func<string, bool> filteringStrategy);
        
        //public void RegisterMigrationHandler(IMigrationHandler migrationHandler);

        void CleanAllSaves();
    }

    internal class Caravan : ICaravan
    {
        private readonly ISubstitutesFinder _substitutesFinder;
        private readonly ISerializer _serializer;
        private readonly SortedList<int, IMigrationHandler> _migrationHandlers;
        private readonly ICaravanObjectTransformer _caravanObjectTransformer;
        private Dictionary<Type, List<Action<ScriptableObject>>> _beforeSaveEvents;
        
        public SortedList<int, IMigrationHandler> MigrationHandlers => _migrationHandlers;
        public event Action OnAllSaved;
        public event Action OnAllLoaded;
        public event Action<ScriptableObject> BeforeSave;

        public Caravan()
        {
            _migrationHandlers = new SortedList<int, IMigrationHandler>();
            _substitutesFinder = new SubstitutesFinder();
            _beforeSaveEvents = new Dictionary<Type, List<Action<ScriptableObject>>>();
            
            //TODO pass the serializer via editor settings ui + add options instead of define ?
            #if UNITY_EDITOR && !CARAVAN_USE_BASE64_ENCODING
            _serializer = new JsonSerializer();
            #else
             _serializer = new Base64EncodedSerializer<JsonSerializer>(new JsonSerializer());
            #endif
            _caravanObjectTransformer = new CaravanObjectTransformer(_substitutesFinder);
        }
        
        public void RegisterMigrationHandler(IMigrationHandler migrationHandler)
        {
            _migrationHandlers.Add(migrationHandler.Version, migrationHandler);
        }

        public void SaveAll()
        {
            Dictionary<string, List<ScriptableObject>> savedByFile = 
                GetValidScriptablesWithSavedAttributeGroupedByFile();
            
            foreach (var (fileName, saveds) in 
                     savedByFile.Select(x => (x.Key, x.Value)))
            {
                var caravanFile = new CaravanFile(fileName, GetLatestVersionForSave());
                
                PopulateCaravanFile(saveds, caravanFile);
                _serializer.Serialize(BuildSavePath(fileName), caravanFile);
            }

            OnAllSaved?.Invoke();
            
            void CheckIdForCorrectness(object scriptableObject, CaravanIdAttribute id, FieldInfo field)
            {
                if (id == null)
                {
                    throw new UnityException("An object tagged Saved, must have a CaravanId defined.");
                }
                else if (field.GetCustomAttributes<CaravanIdAttribute>().Count() != 1)
                {
                    throw new UnityException("Only 1 CaravanId should be declared in the file");
                }
                else if (field.GetValue(scriptableObject)?.GetType() != typeof(string))
                {
                    throw new UnityException("A CaravanId must be a String");
                }
                else if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    throw new UnityException("The CaravanId must also be SerializeField.");
                }
                else if (string.IsNullOrEmpty(field.GetValue<string>(scriptableObject)))
                {
                    throw new UnityException("A CaravanId must not be null or empty.");
                }

                //ID Should be good at this point
                var idVal = field.GetValue<string>(scriptableObject);
                CheckDuplicatedIDs((i) => i == idVal);
            }
            
            void PopulateCaravanFile(List<ScriptableObject> scriptables, CaravanFile caravanFile)
            {
                foreach (var so in scriptables)
                {
                    BeforeSave?.Invoke(so);
                    so.FindIdAttributeAndField(out var idAttribute, out var idField);
                    CheckIdForCorrectness(so, idAttribute, idField);

                    //TODO check no dupped Ids accross all objects
                    var nestedCaravanObjects = _caravanObjectTransformer.CaravanObjectFromGameData(so);
                    caravanFile.Add(nestedCaravanObjects.First());
                }
            }
        }
       
        public void LoadAll()
        {
           
            Dictionary<string, List<ScriptableObject>> savedByFile = 
                GetValidScriptablesWithSavedAttributeGroupedByFile();
            
            foreach (var (key, scriptables) 
                     in savedByFile
                         .Select(x => (x.Key, x.Value)))
            {
                var fileName = key;
                string filePath = BuildSavePath(fileName);
                var caravanFile = _serializer.Deserialize(filePath);
                LoadFromCaravanFile(caravanFile, scriptables);
            }

            OnAllLoaded?.Invoke();

            void LoadFromCaravanFile(CaravanFile caravanFile, List<ScriptableObject> scriptables)
            {
                if (caravanFile != null)
                {
                    foreach (var so in scriptables)
                    {
                        var idValue = so.GetId();
                        var currentCaravanObj = caravanFile.CaravanObjects.Find(co => co.Id == idValue);
                        var handlers = FindMigrationHandlers(caravanFile, so);

                        if (currentCaravanObj != null) //should not be unless the save is not consistent anymore
                        {
                            _caravanObjectTransformer.CaravanObjectToGameData(so, currentCaravanObj);

                            //Handle migrations if needed
                            foreach (var handler in handlers)
                            {
                                handler.FindDefinitionForType(so.GetType())
                                    .Migrate(new Loader(_caravanObjectTransformer, currentCaravanObj), so);
                            }
                        }
                    }
                }
            }
        }

        public bool HasSave()
        {
            var files = Directory
                .EnumerateFiles(GetSaveFolder())
                .Where(f => f.EndsWith(_serializer.GetExtension()))
                .ToList();

            return files.Count != 0;
        }

        public void GenerateAllMissingInstanceID()
        {
            foreach (var so in CaravanHelpers.GetAllScriptablesTaggedSaved())
            {
                so.FindIdAttributeAndField(out _, out var field);
                var idValue = field.GetValue<string>(so);
                if (string.IsNullOrEmpty(idValue))
                {
                    field.SetValue(so, Guid.NewGuid().ToString());
                }
            }
        }
        
        public void CheckDuplicatedIDs(Func<string, bool> filteringStrategy)
        {
            var scriptables = CaravanHelpers.GetAllScriptablesTaggedSaved();
            var dic = scriptables
                .Where(so => so.FindIdField() != null)
                .Select(so => (So: so, IdField: so.FindIdField()))
                .Where(t => filteringStrategy?.Invoke(t.So.GetId()) ?? true)
                .Select(t => (So: t.So, Id: t.So.GetId()))
                .GroupBy(t => t.Id, tt => tt.So)
                .ToDictionary(grp => grp.Key, gg => gg.ToList());

            foreach (var keyValuePair in dic)
            {
                if (keyValuePair.Value.Count > 1)
                {
                    var so = keyValuePair.Value[0];
                    var duplicatedId = so.GetId();
                    Debug.LogError($"Duplicated id : {duplicatedId}");
                    
#if UNITY_EDITOR
                    Selection.objects = 
                                keyValuePair.Value
                                            .Select(s => s as Object)
                                            .ToArray();
#endif
                }
            }
        }
        
        public void RegisterToBeforeSaveEvent<T>(Action<ScriptableObject> action) where T : ScriptableObject
        {
            if (_beforeSaveEvents.ContainsKey(typeof(T)))
            {
                _beforeSaveEvents[typeof(T)].Add(action);
            }
            else
            {
                var l = new List<Action<ScriptableObject>>();
                _beforeSaveEvents.Add(typeof(T), l);
                l.Add(action);
            }
        }

        public void UnregisterFromBeforeSaveEvent<T>(Action<ScriptableObject> action) where T : ScriptableObject
        {
            if (_beforeSaveEvents.ContainsKey(typeof(T)))
            {
                _beforeSaveEvents[typeof(T)].Remove(action);
            }
        }

        public void CleanAllSaves()
        {
            var files = Directory
                .EnumerateFiles(GetSaveFolder())
                .Where(f => f.EndsWith(_serializer.GetExtension()))
                .ToList();
            
            if (files.Count == 0)
            {
                Debug.Log("No save files to delete.");
                return;
            }
            
            foreach (var enumerateFile in files)
            {
                File.Delete(enumerateFile);
                Debug.Log($"Deleted {enumerateFile}");
            }
        }

        //Internal
        private List<IMigrationHandler> FindMigrationHandlers(CaravanFile caravanFile, ScriptableObject obj)
        {
            var handlers = _migrationHandlers.Values
                .Where(h => h.Version > caravanFile.Version)
                .Where(h => h.FindDefinitionForType(obj.GetType()) != null)
                .ToList();
            return handlers;
        }

        private static string GetSaveFolder() => Application.persistentDataPath + Path.DirectorySeparatorChar;
        
        private string BuildSavePath(string filename) => $"{GetSaveFolder()}{filename}.{_serializer.GetExtension()}";

        private int GetLatestVersionForSave() => _migrationHandlers.Values.LastOrDefault()?.Version ?? 1;

        private Dictionary<string, List<ScriptableObject>> GetValidScriptablesWithSavedAttributeGroupedByFile()
        {
            var gameObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            var scriptablesGroupedByFile =
                            gameObjects
                                .Select(so => so)
#if UNITY_EDITOR
                                //This will fail anyway in a build, but at least we'll already know while testing in editor.
                                //Don't instantiate SOs at runtime, it's a wasp nest.  
                                .Where(AssetDatabase.Contains)
#endif
                    .Select(so => (Obj: so,
                                                Saved : so.GetType()
                                                          .GetCustomAttributes(false)
                                                          .FirstOrDefault(ca => ca is SavedAttribute) 
                                                    as SavedAttribute))
                                .Where(e => e.Saved != null && e.Saved.Validate())
                                .GroupBy(t => t.Saved.File)
                                .ToDictionary(grp => grp.Key,
                                    grp => grp.Select(tuple => tuple.Obj)
                                                                       .ToList());
            return scriptablesGroupedByFile;
        }
    }
}