namespace CK.MQTT.Sdk.Packets
{
    internal class Disconnect : IPacket
    {
        public MqttPacketType Type => MqttPacketType.Disconnect;
    }
}
