using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tests
{
    [Serializable]
    internal class FooWillMessage
    {
        public string Message { get; set; }

        public byte[] GetPayload()
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using( MemoryStream stream = new MemoryStream() )
            {
                formatter.Serialize( stream, this );

                return stream.ToArray();
            }
        }

        public static FooWillMessage GetMessage( byte[] willPayload )
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using( MemoryStream stream = new MemoryStream( willPayload ) )
            {
                return formatter.Deserialize( stream ) as FooWillMessage;
            }
        }
    }
}
