using System.Text;
using CaravanSerialization.ObjectModel;

namespace CaravanSerialization.Serialization
{
    internal interface IEncryptionAwareSerializer : ISerializer
    {
        public delegate string CryptOperation(string toCrypt);

        public Encoding DefaultEncoding { get; }
        public CryptOperation OnEncryptHandler { get; set; }
        public CryptOperation OnDecryptHandler { get; set; }
    }
    
    internal interface ISerializer 
    {
        public void Serialize(string filePath, CaravanFile fileToSave);
        public CaravanFile Deserialize(string fileToLoad);
        string GetExtension();
    }
}