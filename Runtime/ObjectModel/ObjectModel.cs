using System;
using System.Collections.Generic;

namespace CaravanSerialization.ObjectModel
{
    [Serializable]
    internal class CaravanFile
    {
        public int Version;
        public string FileName;
        public List<CaravanObject> CaravanObjects;

        public CaravanFile(string fileName, int version)
        {
            FileName = fileName;
            CaravanObjects = new List<CaravanObject>();
            Version = version;
        }

        public void Add(CaravanObject item)
        {
            CaravanObjects.Add(item);
        }
    }

    [Serializable]
    internal class CaravanObjectData
    {
        public string MemberName;
        public object Data;

        public CaravanObjectData(string memberName, object data)
        {
            MemberName = memberName;
            Data = data;
        }
    }

    [Serializable]
    internal class CaravanObject
    {
        public string Id;
        public List<CaravanObjectData> Fields;
        public List<CaravanObject> Nested;

        public CaravanObject(String id, List<CaravanObjectData> fields, List<CaravanObject> nested)
        {
            Id = id;
            Fields = fields;
            Nested = nested;

            if (Fields == null || Fields.Count == 0)
            {
                Fields = null;
            }
            
            if (Nested == null || Nested.Count == 0)
            {
                Nested = null;
            }
        }
    }
}