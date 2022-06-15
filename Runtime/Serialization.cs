using System;
using System.Collections.Generic;

namespace CaravanSerialization
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
            this.MemberName = memberName;
            this.Data = data;
        }
    }

    [Serializable]
    internal class CaravanObject
    {
        public string Id;
        public List<CaravanObjectData> Fields;
        public List<CaravanObject> Nested;

        public CaravanObject(String id, List<CaravanObjectData> data, List<CaravanObject> nested)
        {
            Id = id;
            Fields = data;
            Nested = nested;
        }
    }

    public interface ISaver
    {
        public void Save<T>(T value, string key);
    }

    public interface ILoader
    {
        public T Load<T>(string key);
    }
}