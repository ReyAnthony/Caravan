using System;
using System.IO;
using System.Text;
using CaravanSerialization.ObjectModel;
using Newtonsoft.Json;
using UnityEngine;
using static CaravanSerialization.Serialization.IEncryptionAwareSerializer;

namespace CaravanSerialization.Serialization
{
    internal class JsonSerializer : IEncryptionAwareSerializer
    {
        public void Serialize(string filePath, CaravanFile fileToSave)
        {
            var options = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(fileToSave, Formatting.Indented, options);
            json = OnEncryptHandler?.Invoke(json) ?? json;
            
            File.WriteAllText(filePath, json, this. DefaultEncoding);
            Debug.Log("[Caravan] Saved " + filePath);
        }

        public CaravanFile Deserialize(string fileToLoad)
        {
            try
            {
                var json = File.ReadAllText(fileToLoad, Encoding.Unicode);
                json = OnDecryptHandler?.Invoke(json) ?? json;
                
                var options = new JsonSerializerSettings();
                return JsonConvert.DeserializeObject<CaravanFile>(json, options);
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return null;
            }
        }

        public Encoding DefaultEncoding => Encoding.Unicode;
        public CryptOperation OnEncryptHandler { get; set; }
        public CryptOperation OnDecryptHandler { get; set; }
    }
}