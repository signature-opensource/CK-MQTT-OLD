namespace CK.MQTT.Common.Packets
{
    public interface IPacket
    {
        MqttPacketType Type { get; }
    }
}
