using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal interface IProtocolFlow
    {
        Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel );
    }
}
