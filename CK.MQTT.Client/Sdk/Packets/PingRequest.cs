namespace CK.MQTT.Sdk.Packets
{
    internal class PingRequest : IPacket
    {
        public MqttPacketType Type => MqttPacketType.PingRequest;
    }
}
