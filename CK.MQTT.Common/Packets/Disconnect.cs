namespace CK.MQTT.Common.Packets
{
    internal class Disconnect : IPacket
    {
        public MqttPacketType Type => MqttPacketType.Disconnect;
    }
}
