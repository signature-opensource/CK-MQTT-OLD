using CK.MQTT;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class ProtocolEncodingSpec
    {
        [Test]
        public void when_encoding_string_then_prefix_length_is_added()
        {
            string text = "Foo";
            byte[] encoded = MqttProtocol.Encoding.EncodeString( text );

            (MqttProtocol.StringPrefixLength + text.Length).Should().Be( encoded.Length );
            0x00.Should().Be( encoded[0] );
            0x03.Should().Be( encoded[1] );
        }

        [Test]
        public void when_encoding_string_with_exceeded_length_then_fails()
        {
            string text = GetRandomString( size: 65537 );

            Assert.Throws<MqttException>( () => MqttProtocol.Encoding.EncodeString( text ) );
        }

        [Test]
        public void when_encoding_int32_minor_than_max_protocol_length_then_is_encoded_big_endian()
        {
            int number = 35000; //00000000 00000000 10001000 10111000
            byte[] encoded = MqttProtocol.Encoding.EncodeInteger( number );

            2.Should().Be( encoded.Length );
            Convert.ToByte( "10001000", fromBase: 2 ).Should().Be( encoded[0] );
            Convert.ToByte( "10111000", fromBase: 2 ).Should().Be( encoded[1] );
        }

        [Test]
        public void when_encoding_uint16_then_succeeds_is_encoded_big_endian()
        {
            ushort number = 35000; //10001000 10111000
            byte[] encoded = MqttProtocol.Encoding.EncodeInteger( number );

            2.Should().Be( encoded.Length );
            Convert.ToByte( "10001000", fromBase: 2 ).Should().Be( encoded[0] );
            Convert.ToByte( "10111000", fromBase: 2 ).Should().Be( encoded[1] );
        }

        [Test]
        public void when_encoding_int32_major_than_max_protocol_length_then_fails()
        {
            int number = 310934; //00000000 00000100 10111110 10010110

            Assert.Throws<MqttException>( () => MqttProtocol.Encoding.EncodeInteger( number ) );
        }

        [Test]
        public void when_encoding_remaining_length_then_succeeds()
        {
            //According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

            int length1From = 0; //00000000
            int length1To = 127; //01111111

            int length2From = 128; //00000000 10000000
            int length2To = 16383; //00111111 11111111

            int length3From = 16384; //00000000 01000000 00000000
            int length3To = 2097151; //00011111 11111111 11111111

            int length4From = 2097152; //00000000 00100000 00000000 00000000
            int length4To = 268435455; //00001111 11111111 11111111 11111111

            int length5 = 64; //01000000
            int length6 = 321; //00000001 01000001

            byte[] encoded1From = MqttProtocol.Encoding.EncodeRemainingLength( length1From );
            byte[] encoded1To = MqttProtocol.Encoding.EncodeRemainingLength( length1To );

            byte[] encoded2From = MqttProtocol.Encoding.EncodeRemainingLength( length2From );
            byte[] encoded2To = MqttProtocol.Encoding.EncodeRemainingLength( length2To );

            byte[] encoded3From = MqttProtocol.Encoding.EncodeRemainingLength( length3From );
            byte[] encoded3To = MqttProtocol.Encoding.EncodeRemainingLength( length3To );

            byte[] encoded4From = MqttProtocol.Encoding.EncodeRemainingLength( length4From );
            byte[] encoded4To = MqttProtocol.Encoding.EncodeRemainingLength( length4To );

            byte[] encoded5 = MqttProtocol.Encoding.EncodeRemainingLength( length5 ); //0x40
            byte[] encoded6 = MqttProtocol.Encoding.EncodeRemainingLength( length6 ); //193 2

            1.Should().Be( encoded1From.Length );
            0x00.Should().Be( encoded1From[0] );
            1.Should().Be( encoded1To.Length );
            0x7F.Should().Be( encoded1To[0] );

            2.Should().Be( encoded2From.Length );
            0x80.Should().Be( encoded2From[0] );
            0x01.Should().Be( encoded2From[1] );
            2.Should().Be( encoded2To.Length );
            0xFF.Should().Be( encoded2To[0] );
            0x7F.Should().Be( encoded2To[1] );

            3.Should().Be( encoded3From.Length );
            0x80.Should().Be( encoded3From[0] );
            0x80.Should().Be( encoded3From[1] );
            0x01.Should().Be( encoded3From[2] );
            3.Should().Be( encoded3To.Length );
            0xFF.Should().Be( encoded3To[0] );
            0xFF.Should().Be( encoded3To[1] );
            0x7F.Should().Be( encoded3To[2] );

            4.Should().Be( encoded4From.Length );
            0x80.Should().Be( encoded4From[0] );
            0x80.Should().Be( encoded4From[1] );
            0x80.Should().Be( encoded4From[2] );
            0x01.Should().Be( encoded4From[3] );
            4.Should().Be( encoded4To.Length );
            0xFF.Should().Be( encoded4To[0] );
            0xFF.Should().Be( encoded4To[1] );
            0xFF.Should().Be( encoded4To[2] );
            0x7F.Should().Be( encoded4To[3] );

            1.Should().Be( encoded5.Length );
            0x40.Should().Be( encoded5[0] );

            2.Should().Be( encoded6.Length );
            193.Should().Be( encoded6[0] );
            2.Should().Be( encoded6[1] );
        }

        [Test]
        public void when_decoding_remaining_length_then_succeeds()
        {
            //According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

            List<byte> encoded1From = new List<byte> { 0x10, 0x00 };


            List<byte> encoded1To = new List<byte> { 0x10, 0x7F };


            List<byte> encoded2From = new List<byte> { 0x10, 0x80, 0x01 };


            List<byte> encoded2To = new List<byte> { 0x10, 0xFF, 0x7F };


            List<byte> encoded3From = new List<byte> { 0x10, 0x80, 0x80, 0x01 };


            List<byte> encoded3To = new List<byte> { 0x10, 0xFF, 0xFF, 0x7F };


            List<byte> encoded4From = new List<byte>
            {
                0x10,
                0x80,
                0x80,
                0x80,
                0x01
            };


            List<byte> encoded4To = new List<byte>
            {
                0x10,
                0xFF,
                0xFF,
                0xFF,
                0x7F
            };


            List<byte> bytes1 = new List<byte> { 0x10, 0x40 };


            List<byte> bytes2 = new List<byte> { 0x10, 193, 2 };


            int length1From = MqttProtocol.Encoding.DecodeRemainingLength( encoded1From.ToArray(), out int arrayLength1From ); //0
            int length1To = MqttProtocol.Encoding.DecodeRemainingLength( encoded1To.ToArray(), out int arrayLength1To ); //127

            int length2From = MqttProtocol.Encoding.DecodeRemainingLength( encoded2From.ToArray(), out int arrayLength2From ); //128
            int length2To = MqttProtocol.Encoding.DecodeRemainingLength( encoded2To.ToArray(), out int arrayLength2To ); //16383

            int length3From = MqttProtocol.Encoding.DecodeRemainingLength( encoded3From.ToArray(), out int arrayLength3From ); //16384
            int length3To = MqttProtocol.Encoding.DecodeRemainingLength( encoded3To.ToArray(), out int arrayLength3To ); //2097151

            int length4From = MqttProtocol.Encoding.DecodeRemainingLength( encoded4From.ToArray(), out int arrayLength4From ); //2097152
            int length4To = MqttProtocol.Encoding.DecodeRemainingLength( encoded4To.ToArray(), out int arrayLength4To ); //268435455

            int length1 = MqttProtocol.Encoding.DecodeRemainingLength( bytes1.ToArray(), out int arrayLength1 ); //64
            int length2 = MqttProtocol.Encoding.DecodeRemainingLength( bytes2.ToArray(), out int arrayLength2 ); //321

            1.Should().Be( arrayLength1From );
            0.Should().Be( length1From );
            1.Should().Be( arrayLength1To );
            127.Should().Be( length1To );

            2.Should().Be( arrayLength2From );
            128.Should().Be( length2From );
            2.Should().Be( arrayLength2To );
            16383.Should().Be( length2To );

            3.Should().Be( arrayLength3From );
            16384.Should().Be( length3From );
            3.Should().Be( arrayLength3To );
            2097151.Should().Be( length3To );

            4.Should().Be( arrayLength4From );
            2097152.Should().Be( length4From );
            4.Should().Be( arrayLength4To );
            268435455.Should().Be( length4To );

            1.Should().Be( arrayLength1 );
            64.Should().Be( length1 );

            2.Should().Be( arrayLength2 );
            321.Should().Be( length2 );
        }

        [Test]
        public void when_decoding_malformed_remaining_length_then_fails()
        {
            List<byte> bytes = new List<byte>
            {
                0x10,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0x7F
            };
            //According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213
            Assert.Throws<MqttException>( () => MqttProtocol.Encoding.DecodeRemainingLength( bytes.ToArray(), out _ ) );
        }

        static string GetRandomString( int size )
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] buffer = new char[size];

            for( int i = 0; i < size; i++ )
            {
                buffer[i] = chars[random.Next( chars.Length )];
            }

            return new string( buffer );
        }
    }
}
