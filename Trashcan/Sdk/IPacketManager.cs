
using CK.Core;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketManager
    {
        Task<Mon<IPacket>> GetPacketAsync( Mon<byte[]> bytes );

        Task<Mon<byte[]>> GetBytesAsync( Mon<IPacket> packet );
    }
}
