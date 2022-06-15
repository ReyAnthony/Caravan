using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

//TODO use Anonymous Types instead of Tuples ?
//https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/anonymous-types
namespace CaravanSerialization
{
    public interface ICaravan
    {
        public event Action OnAllSaved;
        public event Action OnAllLoaded;

        public IMapperFinder MapperFinder { get; }

        public void SaveAll();
        public void LoadAll();
        public void GenerateAllMissingInstanceID();
        public void RegisterMigrationHandler(IMigrationHandler migrationHandler);
    }

    //TODO Reload if domain reload is disabled
    public static class CaravanHelper
    {
        private static ICaravan _instance;

        //Call this at start of the game
        //If you need to avoid delay due to lazy loading
        public static void Preload()
        {
            var i = Instance;
        }

        //Use this only if you don't use DI
        public static ICaravan Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Caravan();
                }
                return _instance;
            }
        }
    }

    internal class Caravan : ICaravan
    {
        //Classes
        private class Saver : ISaver
        {
            private List<CaravanObjectData> _currentObject;
            private readonly IMapperFinder _mapperFinder;

            public Saver(List<CaravanObjectData> currentObject, IMapperFinder mapperFinder)
            {
                _currentObject = currentObject;
                this._mapperFinder = mapperFinder;
            }

            public void Save<T>(T value, string key)
            {
                var convertedVal = _mapperFinder.FindMapper(typeof(T))?.Serialize(value);
                if (convertedVal == null)
                {
                    throw new UnityException($"Caravan could not find Mapper for the following type : {typeof(T)}");
                }
                _currentObject.Add(new CaravanObjectData(key, convertedVal));
            }
        }

        private class Loader : ILoader
        {
            private List<CaravanObjectData> _currentObject;
            private IMapperFinder _mapperFinder;

            public Loader(List<CaravanObjectData> currentObject, IMapperFinder mapperFinder)
            {
                _mapperFinder = mapperFinder;
                _currentObject = currentObject;
            }

            public T Load<T>(string key)
            {
                var data = _currentObject.Find(co => co.MemberName == key)?.Data;
                if (data == null)
                {
                    throw new UnityException($"{key} could not be found while loading.");
                }

                var loaded = (T)_mapperFinder.FindMapper(typeof(T))?.Deserialize(data);
                if (loaded == null)
                {
                    throw new UnityException($"Caravan could not find Mapper for the following type : {typeof(T)}");
                }
                return loaded;

            }
        }

        private struct ObjectMetadata
        {
            public Type Type;
            public object Object;

            public ObjectMetadata(Type type, object @object)
            {
                Type = type;
                Object = @object;
            }
        }

        //Actual class
        private IMapperFinder _mapperFinder;
        private SortedList<int, IMigrationHandler> _migrationHandlers;

        public IMapperFinder MapperFinder => _mapperFinder;
        public SortedList<int, IMigrationHandler> MigrationHandlers => _migrationHandlers;

        public event Action OnAllSaved;
        public event Action OnAllLoaded;

        public Caravan()
        {
            _mapperFinder = new TypeMapperFinder();
            _migrationHandlers = new();
        }

        public void RegisterMigrationHandler(IMigrationHandler migrationHandler)
        {
            _migrationHandlers.Add(migrationHandler.Version, migrationHandler);
        }

        public void SaveAll()
        {
            var scriptableObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            var metadataGroupedByFile = MetadataGroupedByFile(scriptableObjects);

            foreach (var fileAndMetadata in metadataGroupedByFile)
            {
                var fileName = fileAndMetadata.Key;

                //TODO manage versions
                var fileToSave = new CaravanFile(fileName, GetLatestVersionForSave());
              
                foreach (var data in fileAndMetadata.Value)
                {

                    FindIdAndField(data, out var id, out var field);
                    CheckIdForCorrectness(data.Object, id, field);
                    var idValue = (string)field.GetValue(data.Object);

                    //TODO check no dupped Ids accross all objects

                    var nested = new List<CaravanObject>();
                    RecursiveSaveTraversal(data.Object, idValue, nested, out var _, isRoot: true);
                    fileToSave.Add(nested.First());
                }

                var options = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto             
                };

                var json = JsonConvert.SerializeObject(fileToSave, Formatting.Indented, options);
                var filePath = BuildSavePath(fileName);

                File.WriteAllText(filePath, json);
                Debug.Log("[Caravan] Saved " + filePath);
            }

            OnAllSaved?.Invoke();

            void RecursiveSaveTraversal(object root, string id, List<CaravanObject> rootNested, out bool failedFindingNestedAttribute, bool isRoot = false)
            {
                //If there's a nested attribute OR we are at the root and not a nested
                if (!isRoot && root.GetType().GetCustomAttribute<NestedAttribute>() == null)
                {
                    failedFindingNestedAttribute = true;
                    return;
                }

                var nestedObjects = new List<CaravanObject>();
                var objectDatas = new List<CaravanObjectData>();

                var metadata = new ObjectMetadata(root.GetType(), root);
                var saveThatFields = GetAllSaveThatFields(metadata);
                foreach (var (Field, Attr) in saveThatFields)
                {
                    //What happens if the object is null ?
                    object o = Field.GetValue(root);

                    var wrapped = _mapperFinder.FindMapper(Field.FieldType)?.Serialize(o);

                    //Maybe we need to go into a nested
                    if (wrapped == null)
                    {
                        //the id of the nested is the name of the field
                        RecursiveSaveTraversal(o, Field.Name, nestedObjects, out var failedFindingNested, isRoot: false);
                        if(failedFindingNested)
                        {
                            throw new UnityException($"Caravan could not find Mapper or [Nested] for the following type : {o.GetType()}");
                        }
                    }
                    else
                    {
                        objectDatas.Add(new CaravanObjectData(Field.Name, wrapped));
                    }
                }

                //Check no dupped callback
                GetCallback<SaveCallback, ISaver>(root)?.Invoke(new Saver(objectDatas, _mapperFinder));
                CheckForDuppedMembers(metadata, objectDatas);

                rootNested.Add(new CaravanObject(id, objectDatas, nestedObjects));
                failedFindingNestedAttribute = false;
            }

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
                else if (field.GetValue(scriptableObject).GetType() != typeof(string))
                {
                    throw new UnityException("A CaravanId must be a String");
                }
                else if (field.GetCustomAttribute<SerializeField>() == null)
                {
                    throw new UnityException("The CaravanId must also be SerializeField.");
                }
                else if (string.IsNullOrEmpty((string)field.GetValue(scriptableObject)))
                {
                    throw new UnityException("A CaravanId must not be null or empty.");
                }
            }
            
            void CheckForDuppedMembers(ObjectMetadata data, List<CaravanObjectData> dataToAddToTheFile)
            {
                if (dataToAddToTheFile.GroupBy(c => c.MemberName).Count() != dataToAddToTheFile.Count())
                {
                    throw new UnityException($"There is a dupped member in : {data.Object.GetType().Name}");
                }
            }
        }
       
        public void LoadAll()
        {
            var gameObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            var metadataGroupedByFile = MetadataGroupedByFile(gameObjects);
            foreach (var kvp in metadataGroupedByFile)
            {
                string filePath = BuildSavePath(kvp.Key);
                CaravanFile caravanFile = null;

                try
                {
                    var json = File.ReadAllText(filePath);
                    var options = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Auto
                    };

                    caravanFile = JsonConvert.DeserializeObject<CaravanFile>(json, options);
                }
                catch (Exception)
                {
                    //no file, maybe we just did not save anything yet.
                    continue;
                }

                if (caravanFile != null)
                {
                    foreach (var metadata in kvp.Value)
                    {
                        FindIdAndField(metadata, out var id, out var field);

                        var currentCaravanObj = caravanFile.CaravanObjects.Find(co => co.Id == (string)field.GetValue(metadata.Object));
                        var handlers = _migrationHandlers.Values
                                                .Where(h => h.Version > caravanFile.Version)
                                                .Where(h => h.FindDefinitionForType(metadata.Type) != null)
                                                .ToList();

                        if (currentCaravanObj != null) //should not be unless the save is no more consistent
                        {
                            RecursiveTraversal(kvp.Key, metadata, currentCaravanObj);
                        }

                        //Handle migrations if needed
                        foreach(var handler in handlers)
                        {
                            handler.FindDefinitionForType(metadata.Object.GetType())
                                .Migrate(new Loader(currentCaravanObj.Fields, _mapperFinder), metadata.Object);
                        }
                    }
                }
            }

            OnAllLoaded?.Invoke();

            void RecursiveTraversal(string fileName, ObjectMetadata metadata, CaravanObject currentCaravanObj)
            {
                foreach (var (Field, Attr) in GetAllSaveThatFields(metadata))
                {
                    var savedData = currentCaravanObj.Fields.Find(f => f.MemberName == Field.Name);

                    //member is missing in the file
                    //So, check in the nested
                    if (savedData == null)
                    {
                        var cobj = currentCaravanObj.Nested.Find(f => f.Id == Field.Name);
                        if(cobj != null)
                        {
                            //What happens if the field object is null ?
                            var fieldObject = Field.GetValue(metadata.Object);
                            RecursiveTraversal(fileName, new ObjectMetadata(Field.FieldType, fieldObject), cobj);
                        }
                        else
                        {
                            Debug.LogWarning($"{fileName}.json : {Field.Name} is missing in the save file.");
                        }
                        continue;
                    }

                    var data = _mapperFinder.FindMapper(savedData.Data.GetType())?.Deserialize(savedData.Data);
                    if (data == null)
                    {
                        throw new UnityException($"Caravan could not find Mapper or [Nested] for the following type : {savedData.Data.GetType()}");
                    }
                    Field.SetValue(metadata.Object, data);
                }

                //Check no dupped callbacks
                GetCallback<LoadCallback, ILoader>(metadata.Object).Invoke(new Loader(currentCaravanObj.Fields, _mapperFinder));
            }
        }

        public void GenerateAllMissingInstanceID()
        {

        }

        //Internal
        private string BuildSavePath(string filename)
        {
            return Application.persistentDataPath + Path.DirectorySeparatorChar + filename + ".json";
        }

        private int GetLatestVersionForSave()
        {
            return _migrationHandlers.Values.LastOrDefault()?.Version ?? 1;
        }

        private List<(FieldInfo Field, SaveThatAttribute Attr)> GetAllSaveThatFields(ObjectMetadata data)
        {
            return data.Type
                        .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        .Select(f => (Field: f, Attr: f.GetCustomAttribute<SaveThatAttribute>()))
                        .Where(tp => tp.Attr != null)
                        .Where(tp => tp.Field.GetValue(data.Object) != null)
                        .ToList();
        }

        private void FindIdAndField(ObjectMetadata data, out CaravanIdAttribute id, out FieldInfo field)
        {
            var d = FindFieldUpTheTypeHierarchy<CaravanIdAttribute>(data.Type);

            id = d.Item1;
            field = d.Item2;
        }

        private Dictionary<string, List<ObjectMetadata>> MetadataGroupedByFile(ScriptableObject[] gameObjects)
        {
            var fileAndData =
                            gameObjects
                                .Select(so => (So: so, Type: so.GetType()))
#if UNITY_EDITOR
                                //This will fail anyway in a build, but at least we'll already know while testing in editor.
                                //Don't instantiate SOs at runtime, it's a wasp nest.  
                                .Where(so => AssetDatabase.Contains(so.So))
#endif
                    .Select(t => (Type: t.Type,
                                        Obj: t.So,
                                        Saved : t.Type.GetCustomAttributes(false)
                                                    .FirstOrDefault(ca => ca is SavedAttribute) as SavedAttribute))
                                .Where(e => e.Saved != null && e.Saved.Validate())
                                .GroupBy(t => t.Saved.File)
                                .ToDictionary(grp => grp.Key, grp => grp.Select(tuple => new ObjectMetadata(tuple.Type, tuple.Obj)).ToList());
            return fileAndData;
        }

        private ValueTuple<T, FieldInfo> FindFieldUpTheTypeHierarchy<T>(Type startType) where T : Attribute
        {
            ValueTuple<T, FieldInfo> AttributeMetadata = (null, null);
            Type t = startType;

            //walk up the type hierarchy to find the CaravanId
            while (AttributeMetadata.Item1 == null && t != typeof(System.Object))
            {
                AttributeMetadata = t
                    .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<T>() != null)
                    .Select(fi => (fi.GetCustomAttribute<T>(), fi))
                    .FirstOrDefault();
                t = t.BaseType;
            }

            return AttributeMetadata;
        }

        private ValueTuple<T, MethodInfo> FindMethodUpTheTypeHierarchy<T>(Type startType) where T : Attribute
        {
            ValueTuple<T, MethodInfo> AttributeMetadata = (null, null);
            Type t = startType;

            //walk up the type hierarchy to find the CaravanId
            while (AttributeMetadata.Item1 == null && t != typeof(System.Object))
            {
                AttributeMetadata = t
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<T>() != null)
                    .Select(fi => (fi.GetCustomAttribute<T>(), fi))
                    .FirstOrDefault();
                t = t.BaseType;
            }

            return AttributeMetadata;
        }

        private Action<T> GetCallback<Attr, T>(object obj) where Attr : Attribute
        {
            MethodInfo m = GetMethodWithAttribute<Attr>(obj);

            //We are not required to implement them
            if (m == null)
            {
                return (T cb) => { };
            }

            if (m.GetParameters().Count() != 1 || m.GetParameters()[0].ParameterType != typeof(T))
            {
                throw new UnityException($" [{typeof(Attr).Name}] needs an {typeof(T).Name} Parameter, see {m.Name}() in {obj.GetType().Name}");
            }

            return (T cb) =>
            {
                object[] @params = { cb };
                m.Invoke(obj, @params);
            };
        }

        private MethodInfo GetMethodWithAttribute<T>(object obj) where T : Attribute
        {
            var result = FindMethodUpTheTypeHierarchy<T>(obj.GetType());
            return result.Item2;
        }
    }
}