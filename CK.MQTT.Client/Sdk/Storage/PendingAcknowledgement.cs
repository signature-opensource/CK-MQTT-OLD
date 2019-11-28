using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk.Storage
{
	internal class PendingAcknowledgement
	{
		public MqttPacketType Type { get; set; }

		public ushort PacketId { get; set; }
	}
}
