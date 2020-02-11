using System;
using System.Linq;
using System.Text;

namespace CK.MQTT.Sdk
{
    internal static class ByteExtensions
    {
        internal static bool IsSet( this byte @byte, int bit )
        {
            if( bit > 7 )
                throw new ArgumentOutOfRangeException( nameof( bit ), ClientProperties.ByteExtensions_InvalidBitPosition );

            return (@byte & (1 << bit)) != 0;
        }

        internal static byte Set( this byte @byte, int bit )
        {
            if( bit > 7 )
                throw new ArgumentOutOfRangeException( nameof( bit ), ClientProperties.ByteExtensions_InvalidBitPosition );

            return Convert.ToByte( @byte | (1 << bit) );
        }

        internal static byte Unset( this byte @byte, int bit )
        {
            if( bit > 7 )
                throw new ArgumentOutOfRangeException( nameof( bit ), ClientProperties.ByteExtensions_InvalidBitPosition );

            return Convert.ToByte( @byte & ~(1 << bit) );
        }

        internal static byte Bits( this byte @byte, int count )
        {
            return Convert.ToByte( @byte >> 8 - count );
        }

        internal static byte Bits( this byte @byte, int index, int count )
        {
            if( index < 1 || index > 8 )
                throw new ArgumentOutOfRangeException( nameof( index ), ClientProperties.ByteExtensions_InvalidByteIndex );

            if( index > 1 )
            {
                int i = 1;

                do
                {
                    @byte = @byte.Unset( 8 - i );
                    i++;
                } while( i < index );
            }

            byte to = Convert.ToByte( @byte << index - 1 );
            byte from = Convert.ToByte( to >> 8 - count );

            return from;
        }

        internal static byte Byte( this byte[] bytes, int index )
        {
            return bytes.Skip( index ).Take( 1 ).FirstOrDefault();
        }

        internal static byte[] Bytes( this byte[] bytes, int fromIndex )
        {
            return bytes.Skip( fromIndex ).ToArray();
        }

        internal static byte[] Bytes( this byte[] bytes, int fromIndex, int count )
        {
            return bytes.Skip( fromIndex ).Take( count ).ToArray();
        }

        internal static string GetString( this byte[] bytes, int index )
        {
            ushort length = bytes.GetStringLenght( index );

            return length == 0 ? string.Empty : Encoding.UTF8.GetString( bytes, index + MqttProtocol.StringPrefixLength, length );
        }

        internal static string GetString( this byte[] bytes, int index, out int nextIndex )
        {
            ushort length = bytes.GetStringLenght( index );

            nextIndex = index + MqttProtocol.StringPrefixLength + length;

            return length == 0 ? string.Empty : Encoding.UTF8.GetString( bytes, index + MqttProtocol.StringPrefixLength, length );
        }

        internal static ushort ToUInt16( this byte[] bytes )
        {
            if( BitConverter.IsLittleEndian )
            {
                Array.Reverse( bytes );
            }

            return BitConverter.ToUInt16( bytes, 0 );
        }

        static ushort GetStringLenght( this byte[] bytes, int index )
        {
            return bytes.Bytes( index, 2 ).ToUInt16();
        }
    }
}
