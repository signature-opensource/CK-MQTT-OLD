using System.Collections.Generic;

namespace CK.MQTT.Sdk.Storage
{
	internal interface IRepository<T>
		 where T : IStorageObject
	{
		IEnumerable<T> ReadAll ();

		T Read (string id);

		void Create (T element);

		void Update (T element);

		void Delete (string id);
	}
}
