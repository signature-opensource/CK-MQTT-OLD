namespace CK.MQTT.Sdk.Packets
{
	internal class Disconnect : IPacket
	{
		public MqttPacketType Type { get { return MqttPacketType.Disconnect; } }
	}
}
