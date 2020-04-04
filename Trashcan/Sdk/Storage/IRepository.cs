#nullable enable
using System.Collections.Generic;

namespace CK.MQTT.Sdk.Storage
{
    internal interface IRepository<T>
         where T : class, IStorageObject
    {
        IEnumerable<T> ReadAll();

        T? Read( string id );

        void Create( T element );

        void Update( T element );

        void Delete( string id );
    }
}
