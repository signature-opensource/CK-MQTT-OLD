using CK.MQTT.Client.Abstractions;
using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    internal interface IPacketChannelFactory
    {
        Task<IMqttChannel<IPacket>> CreateAsync();

        IMqttChannel<IPacket> Create( IMqttChannel<byte[]> binaryChannel );
    }
}
