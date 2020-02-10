using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketManager
    {
        Task<IPacket> GetPacketAsync( byte[] bytes );

        Task<byte[]> GetBytesAsync( IPacket packet );
    }
}
