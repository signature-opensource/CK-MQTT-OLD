namespace CK.MQTT.Common.Packets
{
    internal class PingRequest : IPacket
    {
        public MqttPacketType Type => MqttPacketType.PingRequest;
    }
}
