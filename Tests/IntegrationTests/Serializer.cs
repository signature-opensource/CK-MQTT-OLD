using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace IntegrationTests
{
    public class Serializer
    {
        public static byte[] Serialize<T>( T message )
        {
            byte[] result = default;

            using( MemoryStream stream = new MemoryStream() )
            {
                BinaryFormatter formatter = new BinaryFormatter();

                formatter.Serialize( stream, message );
                result = stream.ToArray();
            }

            return result;
        }

        public static T Deserialize<T>( byte[] content )
            where T : class
        {
            T result = default;

            using( MemoryStream stream = new MemoryStream( content ) )
            {
                BinaryFormatter formatter = new BinaryFormatter();

                result = formatter.Deserialize( stream ) as T;
            }

            return result;
        }
    }
}
