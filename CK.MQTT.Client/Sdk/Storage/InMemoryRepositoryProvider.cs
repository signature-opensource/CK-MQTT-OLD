using System;
using System.Collections.Concurrent;

namespace CK.MQTT.Sdk.Storage
{
	internal class InMemoryRepositoryProvider : IRepositoryProvider
	{
		readonly ConcurrentDictionary<Type, object> repositories = new ConcurrentDictionary<Type, object> ();

		public IRepository<T> GetRepository<T> ()
			where T : IStorageObject
		{
			return repositories.GetOrAdd (typeof (T), new InMemoryRepository<T> ()) as IRepository<T>;
		}
	}
}
