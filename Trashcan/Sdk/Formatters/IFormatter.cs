using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Formatters
{
    internal interface IFormatter
    {
        MqttPacketType PacketType { get; }

        Task<IPacket> FormatAsync( byte[] bytes );

        Task<byte[]> FormatAsync( IPacket packet );
    }
}
