using CK.Core;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class DisconnectFlow : IProtocolFlow
    {
        readonly IConnectionProvider _connectionProvider;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IRepository<ConnectionWill> _willRepository;

        public DisconnectFlow( IConnectionProvider connectionProvider,
            IRepository<ClientSession> sessionRepository,
            IRepository<ConnectionWill> willRepository )
        {
            _connectionProvider = connectionProvider;
            _sessionRepository = sessionRepository;
            _willRepository = willRepository;
        }

        public async Task ExecuteAsync( IActivityMonitor m, string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Disconnect ) return;

            await Task.Run( () =>
            {
                Disconnect disconnect = input as Disconnect;

                m.Info( ServerProperties.DisconnectFlow_Disconnecting( clientId ) );

                _willRepository.Delete( clientId );

                ClientSession session = _sessionRepository.Read( clientId );

                if( session == null )
                {
                    throw new MqttException( ServerProperties.SessionRepository_ClientSessionNotFound( clientId ) );
                }

                if( session.Clean )
                {
                    _sessionRepository.Delete( session.Id );

                    m.Info( ServerProperties.Server_DeletedSessionOnDisconnect( clientId ) );
                }

                _connectionProvider.RemoveConnection( m, clientId );
            } );
        }
    }
}
