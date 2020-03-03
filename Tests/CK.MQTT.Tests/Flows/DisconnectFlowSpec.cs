using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    public class DisconnectFlowSpec
    {
        [Test]
        public async Task when_sending_disconnect_and_client_session_has_clean_state_then_disconnects_and_delete_will_and_session()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            DisconnectFlow flow = new DisconnectFlow( connectionProvider.Object, sessionRepository.Object, willRepository.Object );

            string clientId = Guid.NewGuid().ToString();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            Disconnect disconnect = new Disconnect();

            ClientSession session = new ClientSession( clientId, clean: true );

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, disconnect, channel.Object );

            willRepository.Verify( r => r.Delete( It.IsAny<string>() ) );
            sessionRepository.Verify( r => r.Delete( It.Is<string>( s => s == session.Id ) ) );
        }

        [Test]
        public async Task when_sending_disconnect_and_client_session_has_persistent_state_then_disconnects_and_preserves_session()
        {
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            DisconnectFlow flow = new DisconnectFlow( connectionProvider.Object, sessionRepository.Object, willRepository.Object );

            string clientId = Guid.NewGuid().ToString();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            Disconnect disconnect = new Disconnect();

            ClientSession session = new ClientSession( clientId, clean: false );

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            await flow.ExecuteAsync(TestHelper.Monitor, clientId, disconnect, channel.Object );

            willRepository.Verify( r => r.Delete( It.IsAny<string>() ) );
            sessionRepository.Verify( r => r.Delete( It.Is<string>( s => s == session.Id ) ), Times.Never );
        }
    }
}
