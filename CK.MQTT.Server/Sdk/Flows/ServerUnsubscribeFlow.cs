using CK.Core;

using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerUnsubscribeFlow : IProtocolFlow
    {
        readonly IRepository<ClientSession> _sessionRepository;

        public ServerUnsubscribeFlow( IRepository<ClientSession> sessionRepository )
        {
            _sessionRepository = sessionRepository;
        }

        public async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Unsubscribe )
            {
                return;
            }

            Unsubscribe unsubscribe = input as Unsubscribe;
            ClientSession session = _sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( ServerProperties.SessionRepository_ClientSessionNotFound( clientId ) );
            }

            foreach( string topic in unsubscribe.Topics )
            {
                ClientSubscription subscription = session.GetSubscriptions().FirstOrDefault( s => s.TopicFilter == topic );

                if( subscription != null )
                {
                    session.RemoveSubscription( subscription );
                }
            }

            _sessionRepository.Update( session );

            await channel.SendAsync( Monitored<IPacket>.Create( m, new UnsubscribeAck( unsubscribe.PacketId ) ) );
        }
    }
}
