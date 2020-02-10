namespace CK.MQTT.Sdk.Packets
{
    internal interface IFlowPacket : IPacket
    {
        ushort PacketId { get; }
    }
}
