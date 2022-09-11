using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CaravanSerialization.Attributes;
using CaravanSerialization.Substitutes;
using UnityEngine;

namespace CaravanSerialization.ObjectModel
{
    internal interface ISaverHandler
    {
        void SaveField(SaveThatAttribute attr, string fieldName, object fieldValue, Type fieldType, 
            List<CaravanObjectData> objectTerminalFields, List<CaravanObject> nestedObjects);
    }

    internal interface ILoaderHandler
    {
        void LoadField(SaveThatAttribute attr, object containerObject, 
            string keyToFind,
            CaravanObject currentCaravanObj1, FieldInfo fieldInfo);
    }
    internal interface ICaravanObjectTransformer : ISaverHandler, ILoaderHandler
    { 
        void CaravanObjectToGameData(ScriptableObject currentObj, CaravanObject currentCaravanObj);
        public List<CaravanObject> CaravanObjectFromGameData(ScriptableObject currentObj);
    }

    internal class CaravanObjectTransformer : ICaravanObjectTransformer
    {
        private ISubstitutesFinder _substitutesFinder;

        public CaravanObjectTransformer(ISubstitutesFinder substitutesFinder)
        {
            _substitutesFinder = substitutesFinder;
        }

        //Loading
        public void CaravanObjectToGameData(ScriptableObject currentObj, CaravanObject currentCaravanObj)
        {
            CaravanObjectToGameData((object) currentObj, currentCaravanObj);
        }
        private void CaravanObjectToGameData(object currentObj, CaravanObject currentCaravanObj)
        {
            foreach (var (field, attr) in currentObj.GetAllSaveThatFields())
            {
                LoadField(attr, currentObj, field.Name, currentCaravanObj, field);
            }
            
            currentObj.GetCallback<LoadCallback, ILoader>().Invoke(new Loader(this, currentCaravanObj));
        }

        public void LoadField(SaveThatAttribute attr, object containerObject, string keyToFind, CaravanObject currentCaravanObj1, FieldInfo fieldInfo)
        {
            //TODO hack
            if (keyToFind == nameof(FakeContainer<int>.RESERVED_BY_CARAVAN) 
                && fieldInfo.GetType() != typeof(FakeContainer<>))
            {
                throw new UnityException("wtf bro");
            }
            
            if (attr?.IgnoreTypeSubstitution ?? false)
            {
                HandleTerminalField(containerObject, currentCaravanObj1, fieldInfo);
            }
            else
            {
                var nested = currentCaravanObj1.Nested?.Find(f => f.Id == keyToFind);
                if (nested != null)
                {
                    var fieldObject = fieldInfo.GetValue(containerObject);
                    if (fieldObject.GetType().GetCustomAttribute<NestedAttribute>() != null)
                    {
                        CaravanObjectToGameData(fieldObject, nested);
                    }
                    else if (fieldObject is IList l && l.GetType().IsGenericType)
                    {
                        //TODO handle non generic list ?
                        HandleLists(l, nested);
                    }
                    else
                    {
                        //Substituted types are [Nested] by definition
                        var nextObject = _substitutesFinder.GetSubstitutedTypeFor(fieldInfo.FieldType, fieldObject);
                        //Go inside the replaced object and replace values by those from the file
                        CaravanObjectToGameData(nextObject, nested);
                        var originalType = _substitutesFinder.GetOriginalTypeFor(nextObject.GetType(), nextObject);
                        fieldInfo.SetValue(containerObject, originalType);
                    }
                }
                else
                {
                    Debug.LogWarning($"The field with id {fieldInfo.Name} is missing in the save file.");
                }
            }
            
            void HandleTerminalField(object currentObj, CaravanObject currentCaravanObj, FieldInfo field)
            {
                var savedData = currentCaravanObj.Fields?
                    .Find(f => f.MemberName == field.Name);

                if (savedData != null)
                {
                    //Json .Net is saving some types as the larger versions
                    //TODO specific to JsonNet
                    if (savedData.Data is double && field.FieldType == typeof(float))
                    {
                        savedData.Data = Convert.ToSingle(savedData.Data);
                    }
                    else if (savedData.Data is long && field.FieldType == typeof(int))
                    {
                        savedData.Data = Convert.ToInt32(savedData.Data);
                    }
                    else if (savedData.Data is string && field.FieldType == typeof(char))
                    {
                        savedData.Data = Convert.ToChar(savedData.Data);
                    }

                    field.SetValue(currentObj, savedData.Data);
                }
                else
                {
                    Debug.LogWarning($"For Id {currentCaravanObj.Id}, the field : {field.Name} is missing in the save file.");
                }
            }
            void HandleLists(IList l, CaravanObject nested)
            {
                l.Clear();
                //List has some content
                if (nested.Nested != null)
                {
                    foreach (var caravanObject in nested.Nested)
                    {
                        //Lots of dupped code
                        var listGenericType = l.GetType().GetGenericArguments()[0];
                        object finalObject;

                        if (listGenericType.GetCustomAttribute<NestedAttribute>() != null)
                        {
                            if (listGenericType == typeof(string))
                            {
                                var str = "";
                                CaravanObjectToGameData(str, caravanObject);
                                finalObject = str;
                            }
                            else
                            {
                                var t = Activator.CreateInstance(listGenericType);
                                CaravanObjectToGameData(t, caravanObject);
                                finalObject = t;
                            }
                        }
                        else
                        {
                            var nextObject = _substitutesFinder.GetInstanceOfSubstitutedTypeFor(listGenericType);
                            CaravanObjectToGameData(nextObject, caravanObject);
                            finalObject = _substitutesFinder.GetOriginalTypeFor(nextObject.GetType(), nextObject);
                        }

                        l.Add(finalObject);
                    }
                }
            }
        }
        
        //Saving
        public List<CaravanObject> CaravanObjectFromGameData(ScriptableObject current)
        {
            var idValue = current.GetId();
            var cl = new List<CaravanObject>();
            CaravanObjectFromGameData(current, idValue, cl, true);
            return cl;
        }
        private void CaravanObjectFromGameData(object current, string objectID, List<CaravanObject> rootNested, bool isRoot = false)
        {
            var nestedObjects = new List<CaravanObject>();
            var objectTerminalFields = new List<CaravanObjectData>();
            
            if (current is IList l)
            {
                for (var index = 0; index < l.Count; index++)
                {
                    var o = l[index];
                    SaveField(null, index+"", o, o.GetType(), objectTerminalFields, nestedObjects);
                }
            }
            else
            {
                foreach (var (field, attr) in current.GetAllSaveThatFields())
                {
                    //What happens if the object is null ?
                    var fieldValue = field.GetValue(current);
                    var fieldName = field.Name;
                    var fieldType = field.FieldType;
                    
                    //TODO hack
                    if (fieldName == nameof(FakeContainer<int>.RESERVED_BY_CARAVAN))
                    {
                        throw new UnityException("wtf bro");
                    }
                
                    SaveField(attr, fieldName, fieldValue, fieldType, objectTerminalFields, nestedObjects);
                }
            }
            
            current.GetCallback<SaveCallback, ISaver>()?
                .Invoke(new Saver(this, nestedObjects, objectTerminalFields));
            
            CheckForDuplicatedMembers(current, objectTerminalFields);
            rootNested.Add(new CaravanObject(objectID, fields: objectTerminalFields, nested: nestedObjects));
            void CheckForDuplicatedMembers(object scriptableObject, List<CaravanObjectData> dataToAddToTheFile)
            {
                if (dataToAddToTheFile.GroupBy(c => c.MemberName).Count() != dataToAddToTheFile.Count())
                {
                    throw new UnityException($"There is a dupped member in : {scriptableObject.GetType().Name}");
                }
            }
        }
        public void SaveField(SaveThatAttribute attr, string fieldName, object fieldValue, Type fieldType, 
                                List<CaravanObjectData> objectTerminalFields, List<CaravanObject> nestedObjects)
        {
            //We reached a terminal type that should not be transformed
            if (attr?.IgnoreTypeSubstitution ?? false)
            {
                objectTerminalFields.Add(new CaravanObjectData(fieldName, fieldValue));
            }
            else
            {
                object nextObject;
                if (fieldValue.GetType().GetCustomAttribute<NestedAttribute>() != null
                    || (fieldValue is IList list && list.GetType().IsGenericType))
                {
                    nextObject = fieldValue;
                }
                else
                {
                    nextObject = _substitutesFinder.GetSubstitutedTypeFor(fieldType, fieldValue);
                }

                CaravanObjectFromGameData(nextObject, fieldName, nestedObjects);
            }
        }
    }
}