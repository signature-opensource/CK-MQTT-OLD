namespace CK.MQTT.Sdk.Packets
{
    internal class PingResponse : IPacket
    {
        public MqttPacketType Type => MqttPacketType.PingResponse;
    }
}
