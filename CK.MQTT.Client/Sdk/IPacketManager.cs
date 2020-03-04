
using CK.Core;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketManager
    {
        Task<IMonitored<IPacket>> GetPacketAsync( IMonitored<byte[]> bytes );

        Task<IMonitored<byte[]>> GetBytesAsync( IMonitored<IPacket> packet );
    }
}
