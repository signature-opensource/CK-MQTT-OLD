
using CK.Core;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketManager
    {
        Task<Monitored<IPacket>> GetPacketAsync( Monitored<byte[]> bytes );

        Task<Monitored<byte[]>> GetBytesAsync( Monitored<IPacket> packet );
    }
}
