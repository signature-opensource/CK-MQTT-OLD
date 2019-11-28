using System.Threading.Tasks;
using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Formatters
{
	internal interface IFormatter
	{
		MqttPacketType PacketType { get; }

		Task<IPacket> FormatAsync (byte[] bytes);

		Task<byte[]> FormatAsync (IPacket packet);
	}
}
