using System.Collections.Generic;

namespace CaravanSerialization.ObjectModel
{
    public interface ISaver
    {
        public void Save<T>(T value, string key);
    }
    
    internal class Saver : ISaver
    {
        private readonly ISaverHandler _saverHandler;
        private readonly List<CaravanObject> _nestedObjects;
        private readonly List<CaravanObjectData> _objectTerminalFields;
        
        public Saver(CaravanObjectTransformer saveFieldHandler, 
                        List<CaravanObject> nestedObjects, 
                        List<CaravanObjectData> objectTerminalFields)
        {
            _saverHandler = saveFieldHandler;
            _nestedObjects = nestedObjects;
            _objectTerminalFields = objectTerminalFields;
        }

        public void Save<T>(T value, string key)
        {
            _saverHandler
                .SaveField(null, key, value, typeof(T), _objectTerminalFields, _nestedObjects );
        }
    }
}