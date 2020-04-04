using System;
using System.Collections.Concurrent;

namespace CK.MQTT.Sdk.Storage
{
    internal class InMemoryRepositoryProvider : IRepositoryProvider
    {
        readonly ConcurrentDictionary<Type, object> _repositories = new ConcurrentDictionary<Type, object>();

        public IRepository<T> GetRepository<T>()
            where T : IStorageObject =>
            _repositories.GetOrAdd( typeof( T ), new InMemoryRepository<T>() ) as IRepository<T>;
    }
}
