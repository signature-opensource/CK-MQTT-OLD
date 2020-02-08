using System.Diagnostics;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;
using CK.Core;

namespace CK.MQTT.Sdk.Flows
{
    internal class DisconnectFlow : IProtocolFlow
    {
        readonly IConnectionProvider connectionProvider;
        readonly IRepository<ClientSession> sessionRepository;
        readonly IRepository<ConnectionWill> willRepository;

        public DisconnectFlow( IConnectionProvider connectionProvider,
            IRepository<ClientSession> sessionRepository,
            IRepository<ConnectionWill> willRepository )
        {
            this.connectionProvider = connectionProvider;
            this.sessionRepository = sessionRepository;
            this.willRepository = willRepository;
        }

        public async Task ExecuteAsync(IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Disconnect ) return;

            await Task.Run( () =>
            {
                var monitor = new ActivityMonitor();
                var disconnect = input as Disconnect;

                monitor.Info( string.Format( ServerProperties.DisconnectFlow_Disconnecting, clientId ) );

                willRepository.Delete( clientId );

                var session = sessionRepository.Read( clientId );

                if( session == null )
                {
                    throw new MqttException( string.Format( ServerProperties.SessionRepository_ClientSessionNotFound, clientId ) );
                }

                if( session.Clean )
                {
                    sessionRepository.Delete( session.Id );

                    monitor.Info( string.Format( ServerProperties.Server_DeletedSessionOnDisconnect, clientId ) );
                }

                connectionProvider.RemoveConnection( monitor, clientId );
            } );
        }
    }
}
