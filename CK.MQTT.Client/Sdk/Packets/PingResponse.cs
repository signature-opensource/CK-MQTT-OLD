namespace CK.MQTT.Sdk.Packets
{
	internal class PingResponse : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.PingResponse; } }
	}
}
