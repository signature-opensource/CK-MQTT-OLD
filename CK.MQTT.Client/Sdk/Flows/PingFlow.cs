using CK.Core;

using CK.MQTT.Sdk.Packets;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class PingFlow : IProtocolFlow
    {
        public async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.PingRequest ) return;

            await channel.SendAsync( new Monitored<IPacket>( m, new PingResponse() ) );
        }
    }
}
