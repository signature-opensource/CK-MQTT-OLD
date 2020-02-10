using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class DisconnectFlow : IProtocolFlow
    {
        static readonly ITracer _tracer = Tracer.Get<DisconnectFlow>();

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

        public async Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Disconnect ) return;

            await Task.Run( () =>
            {
                Disconnect disconnect = input as Disconnect;

                _tracer.Info( ServerProperties.Resources.GetString( "DisconnectFlow_Disconnecting" ), clientId );

                _willRepository.Delete( clientId );

                ClientSession session = _sessionRepository.Read( clientId );

                if( session == null )
                {
                    throw new MqttException( string.Format( ServerProperties.Resources.GetString( "SessionRepository_ClientSessionNotFound" ), clientId ) );
                }

                if( session.Clean )
                {
                    _sessionRepository.Delete( session.Id );

                    _tracer.Info( ServerProperties.Resources.GetString( "Server_DeletedSessionOnDisconnect" ), clientId );
                }

                _connectionProvider.RemoveConnection( clientId );
            } );
        }
    }
}
