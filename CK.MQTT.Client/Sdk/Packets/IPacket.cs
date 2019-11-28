namespace CK.MQTT.Sdk.Packets
{
	internal interface IPacket
    {
        MqttPacketType Type { get; }
    }
}
