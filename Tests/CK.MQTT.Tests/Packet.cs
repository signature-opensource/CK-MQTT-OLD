using CK.MQTT.Sdk.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tests
{
    internal class Packet
    {
        internal const string CommentSymbol = "#";

        internal static byte[] ReadAllBytes( string path )
        {
            if( Path.GetExtension( path ) != ".packet" )
            {
                throw new ApplicationException( string.Format( "File extension {0} is invalid. .packet file is expected", Path.GetExtension( path ) ) );
            }

            if( !File.Exists( path ) )
            {
                throw new ApplicationException( string.Format( "The file {0} does not exists", path ) );
            }

            List<byte> bytes = new List<byte>();

            foreach( string line in File.ReadLines( path ).Where( l => !string.IsNullOrEmpty( l ) ) )
            {
                string aux = line;
                int commentIndex = aux.IndexOf( CommentSymbol );

                if( commentIndex != -1 )
                {
                    aux = aux.Substring( 0, commentIndex ).Trim();
                }

                try
                {
                    if( aux.StartsWith( "\"" ) )
                    {
                        aux = aux.Replace( "\"", string.Empty );

                        bytes.AddRange( Encoding.UTF8.GetBytes( aux ) );
                    }
                    else
                    {
                        byte @byte = Convert.ToByte( aux, fromBase: 2 );

                        bytes.Add( @byte );
                    }
                }
                catch
                {
                    continue;
                }
            }

            return bytes.ToArray();
        }

        internal static T ReadPacket<T>( string path ) where T : class, IPacket
        {
            if( Path.GetExtension( path ) != ".json" )
            {
                throw new ApplicationException( string.Format( "File extension {0} is invalid. .json file is expected", Path.GetExtension( path ) ) );
            }

            if( !File.Exists( path ) )
            {
                throw new ApplicationException( string.Format( "The file {0} does not exists", path ) );
            }

            string text = File.ReadAllText( path );

            return Deserialize<T>( text );
        }

        internal static object ReadPacket( string path, Type type )
        {
            if( Path.GetExtension( path ) != ".json" )
            {
                throw new ApplicationException( string.Format( "File extension {0} is invalid. .json file is expected", Path.GetExtension( path ) ) );
            }

            if( !File.Exists( path ) )
            {
                throw new ApplicationException( string.Format( "The file {0} does not exists", path ) );
            }

            string text = File.ReadAllText( path );

            return Deserialize( text, type );
        }

        static T Deserialize<T>( string serialized )
        {
            return JsonConvert.DeserializeObject<T>( serialized, GetConverters( typeof( T ) ).ToArray() );
        }

        static object Deserialize( string serialized, Type type )
        {
            return JsonConvert.DeserializeObject( serialized, type, GetConverters( type ).ToArray() );
        }

        static IEnumerable<JsonConverter> GetConverters( Type type )
        {
            if( type == typeof( Connect ) )
                yield return new MqttLastWillConverter();

            yield return new StringByteArrayConverter();
        }
    }
}
