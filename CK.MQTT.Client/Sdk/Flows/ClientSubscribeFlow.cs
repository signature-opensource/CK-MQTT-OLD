using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ClientSubscribeFlow : IProtocolFlow
    {
        public Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
            => Task.Delay( 0 );
    }
}
