namespace CK.MQTT.Common.Packets
{
    internal interface IFlowPacket : IPacket
    {
        ushort PacketId { get; }
    }
}
