using CK.Core;

using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketChannelFactory
    {
        Task<IMqttChannel<IPacket>> CreateAsync( IActivityMonitor m );

        IMqttChannel<IPacket> Create( IActivityMonitor m, IMqttChannel<byte[]> binaryChannel );
    }
}
