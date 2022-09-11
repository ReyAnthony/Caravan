using System;
using CaravanSerialization.ObjectModel;

namespace CaravanSerialization.Serialization
{
    internal class Base64EncodedSerializer<T> : ISerializer where T : IEncryptionAwareSerializer
    {
        private readonly T _serializer;

        public Base64EncodedSerializer(T serializer)
        {
            _serializer = serializer;
            _serializer.OnDecryptHandler = crypt => _serializer.DefaultEncoding.GetString(Convert.FromBase64String(crypt));
            _serializer.OnEncryptHandler = crypt => Convert.ToBase64String(_serializer.DefaultEncoding.GetBytes(crypt));
        }

        public void Serialize(string filePath, CaravanFile fileToSave) => _serializer.Serialize(filePath, fileToSave);
        public CaravanFile Deserialize(string fileToLoad) => _serializer.Deserialize(fileToLoad);
        public string GetExtension() => _serializer.GetExtension();
    }
}