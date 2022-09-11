using System;
using System.Collections;

namespace CaravanSerialization.ObjectModel
{
    public interface ILoader
    {
        public T Load<T>(string key);
    }
    
    internal class Loader : ILoader
    {
        private readonly ILoaderHandler _loaderHandler;
        private readonly CaravanObject _currentObj;

        public Loader(ILoaderHandler loaderHandler, CaravanObject currentObj)
        {
            _loaderHandler = loaderHandler;
            _currentObj = currentObj;
        }

        public T Load<T>(string key)
        {
            var fakeContainer = new FakeContainer<T>();
            if (typeof(IList).IsAssignableFrom(typeof(T)))
            {
                fakeContainer.RESERVED_BY_CARAVAN = Activator.CreateInstance<T>();
            }
            
            //TODO, this will do for now, but it sucks
            //TODO If you use this name in one of your class, IT WILL BREAK !
            //TODO load field needs a big refactoring, it should return the value and not put it into the field directly
            _loaderHandler.LoadField(null, fakeContainer, key, _currentObj,
                fakeContainer
                    .GetType()
                    .GetField(nameof(fakeContainer.RESERVED_BY_CARAVAN)));
            return fakeContainer.RESERVED_BY_CARAVAN;
        }
    }
    
    internal class FakeContainer<T>
    {
        public T RESERVED_BY_CARAVAN;
    }
}