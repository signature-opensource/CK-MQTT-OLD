using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT.Sdk
{
	public class MqttEncoder
	{
        public static MqttEncoder Default { get; } = new MqttEncoder();

        public byte[] EncodeString (string text)
		{
			var bytes = new List<byte> ();
			var textBytes = Encoding.UTF8.GetBytes (text ?? string.Empty);

			if (textBytes.Length > MqttConstants.MaxIntegerLength) {
				throw new MqttException  (Properties.ProtocolEncoding_StringMaxLengthExceeded);
			}

			var numberBytes = MqttConstants.Encoding.EncodeInteger (textBytes.Length);

			bytes.Add (numberBytes[numberBytes.Length - 2]);
			bytes.Add (numberBytes[numberBytes.Length - 1]);
			bytes.AddRange (textBytes);

			return bytes.ToArray ();
		}

        public byte[] EncodeInteger (int number)
		{
			if (number > MqttConstants.MaxIntegerLength) {
				throw new MqttException  (Properties.ProtocolEncoding_IntegerMaxValueExceeded);
			}

			return EncodeInteger ((ushort)number);
		}

        public byte[] EncodeInteger (ushort number)
		{
			var bytes = BitConverter.GetBytes (number);

			if (BitConverter.IsLittleEndian) {
				Array.Reverse (bytes);
			}

			return bytes;
		}

        public byte[] EncodeRemainingLength (int length)
		{
			var bytes = new List<byte> ();
			var encoded = default(int);

			do {
				encoded = length % 128;
				length = length / 128;

				if (length > 0) {
					encoded = encoded | 128;
				}

				bytes.Add (Convert.ToByte (encoded));
			} while (length > 0);

			return bytes.ToArray ();
		}

        public int DecodeRemainingLength (byte[] packet, out int arrayLength)
		{
			var multiplier = 1;
			var value = 0;
			var index = 0;
			var encodedByte = default(byte);

			do {
				index++;
				encodedByte = packet[index];
				value += (encodedByte & 127) * multiplier;

				if (multiplier > 128 * 128 * 128 || index > 4)
					throw new MqttException  (Properties.ProtocolEncoding_MalformedRemainingLength);

                multiplier *= 128;
            } while ((encodedByte & 128) != 0);

			arrayLength = index;

			return value;
		}
	}
}
