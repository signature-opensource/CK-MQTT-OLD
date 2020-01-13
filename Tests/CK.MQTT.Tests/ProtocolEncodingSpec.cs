using System;
using System.Collections.Generic;
using CK.MQTT;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
	public class ProtocolEncodingSpec
	{
		[Test]
		public void when_encoding_string_then_prefix_length_is_added()
		{
			var text = "Foo";
			var encoded = MqttImplementation.Encoding.EncodeString (text);

			(MqttImplementation.StringPrefixLength + text.Length).Should().Be(encoded.Length);
			0x00.Should().Be(encoded[0]);
			0x03.Should().Be(encoded[1]);
		}

		[Test]
		public void when_encoding_string_with_exceeded_length_then_fails()
		{
			var text = GetRandomString (size: 65537);

			Assert.Throws<MqttException>(() => MqttImplementation.Encoding.EncodeString (text));
		}

		[Test]
		public void when_encoding_int32_minor_than_max_protocol_length_then_is_encoded_big_endian()
		{
			var number = 35000; //00000000 00000000 10001000 10111000
			var encoded = MqttImplementation.Encoding.EncodeInteger (number);

			2.Should().Be(encoded.Length);
			Convert.ToByte ("10001000", fromBase: 2).Should().Be(encoded[0]);
			Convert.ToByte ("10111000", fromBase: 2).Should().Be(encoded[1]);
		}

		[Test]
		public void when_encoding_uint16_then_succeeds_is_encoded_big_endian()
		{
			ushort number = 35000; //10001000 10111000
			var encoded = MqttImplementation.Encoding.EncodeInteger (number);

			2.Should().Be(encoded.Length);
			Convert.ToByte ("10001000", fromBase: 2).Should().Be(encoded[0]);
			Convert.ToByte ("10111000", fromBase: 2).Should().Be(encoded[1]);
		}
		
		[Test]
		public void when_encoding_int32_major_than_max_protocol_length_then_fails()
		{
			var number = 310934; //00000000 00000100 10111110 10010110
			
			Assert.Throws<MqttException>(() => MqttImplementation.Encoding.EncodeInteger (number));
		}

		[Test]
		public void when_encoding_remaining_length_then_succeeds()
		{
			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

			var length1From = 0; //00000000
			var length1To = 127; //01111111

			var length2From = 128; //00000000 10000000
			var length2To = 16383; //00111111 11111111

			var length3From = 16384; //00000000 01000000 00000000
			var length3To = 2097151; //00011111 11111111 11111111

			var length4From = 2097152; //00000000 00100000 00000000 00000000
			var length4To = 268435455; //00001111 11111111 11111111 11111111

			var length5 = 64; //01000000
			var length6 = 321; //00000001 01000001
			
			var encoded1From = MqttImplementation.Encoding.EncodeRemainingLength (length1From);
			var encoded1To = MqttImplementation.Encoding.EncodeRemainingLength (length1To);

			var encoded2From = MqttImplementation.Encoding.EncodeRemainingLength (length2From);
			var encoded2To = MqttImplementation.Encoding.EncodeRemainingLength (length2To);

			var encoded3From = MqttImplementation.Encoding.EncodeRemainingLength (length3From);
			var encoded3To = MqttImplementation.Encoding.EncodeRemainingLength (length3To);

			var encoded4From = MqttImplementation.Encoding.EncodeRemainingLength (length4From);
			var encoded4To = MqttImplementation.Encoding.EncodeRemainingLength (length4To);

			var encoded5 = MqttImplementation.Encoding.EncodeRemainingLength (length5); //0x40
			var encoded6 = MqttImplementation.Encoding.EncodeRemainingLength (length6); //193 2

			1.Should().Be(encoded1From.Length);
			0x00.Should().Be(encoded1From[0]);
			1.Should().Be(encoded1To.Length);
			0x7F.Should().Be(encoded1To[0]);

			2.Should().Be(encoded2From.Length);
			0x80.Should().Be(encoded2From[0]);
			0x01.Should().Be(encoded2From[1]);
			2.Should().Be(encoded2To.Length);
			0xFF.Should().Be(encoded2To[0]);
			0x7F.Should().Be(encoded2To[1]);

			3.Should().Be(encoded3From.Length);
			0x80.Should().Be(encoded3From[0]);
			0x80.Should().Be(encoded3From[1]);
			0x01.Should().Be(encoded3From[2]);
			3.Should().Be(encoded3To.Length);
			0xFF.Should().Be(encoded3To[0]);
			0xFF.Should().Be(encoded3To[1]);
			0x7F.Should().Be(encoded3To[2]);

			4.Should().Be(encoded4From.Length);
			0x80.Should().Be(encoded4From[0]);
			0x80.Should().Be(encoded4From[1]);
			0x80.Should().Be(encoded4From[2]);
			0x01.Should().Be(encoded4From[3]);
			4.Should().Be(encoded4To.Length);
			0xFF.Should().Be(encoded4To[0]);
			0xFF.Should().Be(encoded4To[1]);
			0xFF.Should().Be(encoded4To[2]);
			0x7F.Should().Be(encoded4To[3]);

			1.Should().Be(encoded5.Length);
			0x40.Should().Be(encoded5[0]);

			2.Should().Be(encoded6.Length);
			193.Should().Be(encoded6[0]);
			2.Should().Be(encoded6[1]);
		}

		[Test]
		public void when_decoding_remaining_length_then_succeeds()
		{
			//According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213

            var encoded1From = new List<byte> {0x10, 0x00};


            var encoded1To = new List<byte> {0x10, 0x7F};


            var encoded2From = new List<byte> {0x10, 0x80, 0x01};


            var encoded2To = new List<byte> {0x10, 0xFF, 0x7F};


            var encoded3From = new List<byte> {0x10, 0x80, 0x80, 0x01};


            var encoded3To = new List<byte> {0x10, 0xFF, 0xFF, 0x7F};


            var encoded4From = new List<byte>
            {
                0x10,
                0x80,
                0x80,
                0x80,
                0x01
            };


            var encoded4To = new List<byte>
            {
                0x10,
                0xFF,
                0xFF,
                0xFF,
                0x7F
            };


            var bytes1 = new List<byte> {0x10, 0x40};


            var bytes2 = new List<byte> {0x10, 193, 2};


            var length1From = MqttImplementation.Encoding.DecodeRemainingLength (encoded1From.ToArray(), out var arrayLength1From); //0
            var length1To = MqttImplementation.Encoding.DecodeRemainingLength (encoded1To.ToArray(), out var arrayLength1To); //127

            var length2From = MqttImplementation.Encoding.DecodeRemainingLength (encoded2From.ToArray(), out var arrayLength2From); //128
            var length2To = MqttImplementation.Encoding.DecodeRemainingLength (encoded2To.ToArray(), out var arrayLength2To); //16383

            var length3From = MqttImplementation.Encoding.DecodeRemainingLength (encoded3From.ToArray(), out var arrayLength3From); //16384
            var length3To = MqttImplementation.Encoding.DecodeRemainingLength (encoded3To.ToArray(), out var arrayLength3To); //2097151

            var length4From = MqttImplementation.Encoding.DecodeRemainingLength (encoded4From.ToArray(), out var arrayLength4From); //2097152
            var length4To = MqttImplementation.Encoding.DecodeRemainingLength (encoded4To.ToArray(), out var arrayLength4To); //268435455

            var length1 = MqttImplementation.Encoding.DecodeRemainingLength (bytes1.ToArray(), out var arrayLength1); //64
            var length2 = MqttImplementation.Encoding.DecodeRemainingLength (bytes2.ToArray(), out var arrayLength2); //321

			1.Should().Be(arrayLength1From);
			0.Should().Be(length1From);
			1.Should().Be(arrayLength1To);
			127.Should().Be(length1To);

			2.Should().Be(arrayLength2From);
			128.Should().Be(length2From);
			2.Should().Be(arrayLength2To);
			16383.Should().Be(length2To);

			3.Should().Be(arrayLength3From);
			16384.Should().Be(length3From);
			3.Should().Be(arrayLength3To);
			2097151 .Should().Be(length3To);

			4.Should().Be(arrayLength4From);
			2097152 .Should().Be(length4From);
			4.Should().Be(arrayLength4To);
			268435455 .Should().Be(length4To);

			1.Should().Be(arrayLength1);
			64.Should().Be(length1);

			2.Should().Be(arrayLength2);
			321.Should().Be(length2);
		}

		[Test]
		public void when_decoding_malformed_remaining_length_then_fails()
		{
            var bytes = new List<byte>
            {
                0x10,
                0xFF,
                0xFF,
                0xFF,
                0xFF,
                0x7F
            };
            //According to spec samples: http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html#_Toc385349213
            Assert.Throws<MqttException> (() => MqttImplementation.Encoding.DecodeRemainingLength (bytes.ToArray (), out _));
		}

        static string GetRandomString(int size)
		{
			var random = new Random();
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var buffer = new char[size];

			for (int i = 0; i < size; i++)
			{
				buffer[i] = chars[random.Next(chars.Length)];
			}

			return new string(buffer);
		}
	}
}
