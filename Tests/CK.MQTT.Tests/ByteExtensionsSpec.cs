using CK.MQTT;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class ByteExtensionsSpec
    {
        [Test]
        public void when_setting_bits_then_succeeds()
        {
            byte byte1 = Convert.ToByte( "00100000", fromBase: 2 );
            byte byte2 = Convert.ToByte( "10000000", fromBase: 2 );
            byte byte3 = Convert.ToByte( "00000010", fromBase: 2 );
            byte byte4 = Convert.ToByte( "11111110", fromBase: 2 );

            byte1 = byte1.Set( 3 );
            byte2 = byte2.Set( 1 );
            byte3 = byte3.Set( 2 );
            byte4 = byte4.Set( 0 );

            byte expectedByte1 = Convert.ToByte( "00101000", fromBase: 2 );
            byte expectedByte2 = Convert.ToByte( "10000010", fromBase: 2 );
            byte expectedByte3 = Convert.ToByte( "00000110", fromBase: 2 );
            byte expectedByte4 = Convert.ToByte( "11111111", fromBase: 2 );

            expectedByte1.Should().Be( byte1 );
            expectedByte2.Should().Be( byte2 );
            expectedByte3.Should().Be( byte3 );
            expectedByte4.Should().Be( byte4 );
        }

        [Test]
        public void when_setting_bits_out_of_range_then_fails()
        {
            byte byte1 = Convert.ToByte( "00100000", fromBase: 2 );

            Assert.Throws<ArgumentOutOfRangeException>( () => byte1 = byte1.Set( 8 ) );
        }

        [Test]
        public void when_unsetting_bits_then_succeeds()
        {
            byte byte1 = Convert.ToByte( "00100000", fromBase: 2 );
            byte byte2 = Convert.ToByte( "11111111", fromBase: 2 );
            byte byte3 = Convert.ToByte( "10100010", fromBase: 2 );
            byte byte4 = Convert.ToByte( "00000001", fromBase: 2 );

            byte1 = byte1.Unset( 5 );
            byte2 = byte2.Unset( 7 );
            byte3 = byte3.Unset( 1 );
            byte4 = byte4.Unset( 0 );
            byte1.Should().Be( 0x00 );
            byte2.Should().Be( 0x7F );
            byte3.Should().Be( 0xA0 );
            byte4.Should().Be( 0x00 );
        }

        [Test]
        public void when_unsetting_bits_out_of_range_then_fails()
        {
            byte byte1 = Convert.ToByte( "00100000", fromBase: 2 );

            Assert.Throws<ArgumentOutOfRangeException>( () => byte1 = byte1.Unset( 8 ) );
        }

        [Test]
        public void when_verifying_bit_set_then_succeeds()
        {
            byte @byte = Convert.ToByte( "00100000", fromBase: 2 );

            @byte = @byte.Set( 3 );

            Assert.True( @byte.IsSet( 3 ) );
        }

        [Test]
        public void when_verifying_bit_out_of_range_set_then_fails()
        {
            byte @byte = Convert.ToByte( "00100000", fromBase: 2 );

            @byte = @byte.Set( 3 );

            Assert.Throws<ArgumentOutOfRangeException>( () => @byte.IsSet( 8 ) );
        }

        [Test]
        public void when_getting_bits_then_succeeds()
        {
            byte byte1 = Convert.ToByte( "11101110", fromBase: 2 );
            byte byte2 = Convert.ToByte( "11111111", fromBase: 2 );
            byte byte3 = Convert.ToByte( "00000011", fromBase: 2 );
            byte byte4 = Convert.ToByte( "00110011", fromBase: 2 );

            Convert.ToInt32( byte1.Bits( 4 ) ).Should().Be( 14 );//00001110
            Convert.ToInt32( byte1.Bits( 4, 2 ) ).Should().Be( 1 );//00000001
            Convert.ToInt32( byte2.Bits( 7 ) ).Should().Be( 127 );//01111111
            Convert.ToInt32( byte2.Bits( 7, 2 ) ).Should().Be( 3 );//00000011
            Convert.ToInt32( byte3.Bits( 3 ) ).Should().Be( 0 );//00000000
            Convert.ToInt32( byte3.Bits( 1, 7 ) ).Should().Be( 1 );//00000001
            Convert.ToInt32( byte4.Bits( 3 ) ).Should().Be( 1 );//00000001
            Convert.ToInt32( byte4.Bits( 1, 5 ) ).Should().Be( 6 );//00000110
        }

        [Test]
        public void when_getting_bits_with_index_out_of_range_then_fails()
        {
            byte byte1 = Convert.ToByte( "11101110", fromBase: 2 );

            Assert.Throws<ArgumentOutOfRangeException>( () => byte1.Bits( 0, 2 ) );
            Assert.Throws<ArgumentOutOfRangeException>( () => byte1.Bits( 9, 1 ) );
        }

        [Test]
        public void when_getting_byte_then_succeeds()
        {
            List<byte> bytes = new List<byte>();

            bytes.Add( Convert.ToByte( "00101000", fromBase: 2 ) );
            bytes.Add( 0xFE );
            bytes.Add( 0xA9 );
            bytes.Add( Convert.ToByte( "00000000", 2 ) );

            byte @byte = bytes.ToArray().Byte( 2 );
            @byte.Should().Be( 0XA9 );
        }

        [Test]
        public void when_getting_bytes_then_succeeds()
        {
            List<byte> bytes = new List<byte>();

            bytes.Add( Convert.ToByte( "00101000", fromBase: 2 ) );
            bytes.Add( 0xFE );
            bytes.Add( 0xA9 );
            bytes.Add( Convert.ToByte( "00000000", 2 ) );

            byte[] newBytes = bytes.ToArray().Bytes( 1, 2 );
            newBytes.Length.Should().Be( 2 );
            newBytes[0].Should().Be( 0xFE );
            newBytes[1].Should().Be( 0xA9 );
        }

        [Test]
        public void when_getting_string_then_succeeds()
        {
            List<byte> bytes = new List<byte>();

            bytes.Add( Convert.ToByte( "00101000", fromBase: 2 ) );
            bytes.Add( 0xFE );
            bytes.AddRange( MqttProtocol.Encoding.EncodeString( "Foo" ) );
            bytes.Add( 0xA9 );
            bytes.Add( Convert.ToByte( "00000000", 2 ) );

            string text = bytes.ToArray().GetString( 2 );
            text.Should().Be( "Foo" );
        }

        [Test]
        public void when_getting_string_with_output_then_succeeds()
        {
            List<byte> bytes = new List<byte>();

            bytes.Add( Convert.ToByte( "00101000", fromBase: 2 ) );
            bytes.Add( 0xFE );
            bytes.AddRange( MqttProtocol.Encoding.EncodeString( "Foo" ) );
            bytes.Add( 0xA9 );
            bytes.Add( Convert.ToByte( "00000000", 2 ) );

            string text = bytes.ToArray().GetString( 2, out int nextIndex );

            text.Should().Be( "Foo" );
            nextIndex.Should().Be( 7 );
        }
    }
}
