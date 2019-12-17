namespace CK.MQTT.Sdk.Packets
{
	public interface IPacket
    {
        MqttPacketType Type { get; }
    }
}
