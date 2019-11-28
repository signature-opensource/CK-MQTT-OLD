namespace CK.MQTT.Sdk.Packets
{
	internal class PingRequest : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingRequest; } }
	}
}
