using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reactive.Subjects;

namespace Tests
{
    public class ConnectionProviderSpec
    {
        [Test]
        public void when_registering_private_client_then_private_client_list_increases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            int existingPrivateClients = provider.PrivateClients.Count();
            string clientId = Guid.NewGuid().ToString();

            provider.RegisterPrivateClient( clientId );
            provider.PrivateClients.Should().HaveCount( existingPrivateClients + 1 );
        }

        [Test]
        public void when_registering_existing_private_client_then_fails()
        {
            ConnectionProvider provider = new ConnectionProvider();

            string clientId = Guid.NewGuid().ToString();

            provider.RegisterPrivateClient( clientId );

            MqttServerException ex = Assert.Throws<MqttServerException>( () => provider.RegisterPrivateClient( clientId ) );

            Assert.NotNull( ex );
        }

        [Test]
        public void when_removing_private_connection_then_private_client_list_decreases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == true ) );
            provider.RegisterPrivateClient( clientId );

            int previousPrivateClients = provider.PrivateClients.Count();

            provider.RemoveConnection( clientId );

            int currentPrivateClients = provider.PrivateClients.Count();
            currentPrivateClients.Should().Be( previousPrivateClients - 1 );
        }

        [Test]
        public void when_adding_new_client_then_connection_list_increases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            int existingClients = provider.Connections;
            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == true ) );
            provider.Connections.Should().Be( existingClients + 1 );
        }

        [Test]
        public void when_adding_disconnected_client_then_active_clients_list_does_not_increases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            int existingClients = provider.ActiveClients.Count();
            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == false ) );

            provider.ActiveClients.Should().HaveCount( existingClients );
        }

        [Test]
        public void when_adding_new_client_and_disconnect_it_then_active_clients_list_decreases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            int existingClients = provider.ActiveClients.Count();
            string clientId = Guid.NewGuid().ToString();

            Mock<IMqttChannel<IPacket>> connection = new Mock<IMqttChannel<IPacket>>();

            connection.Setup( c => c.IsConnected ).Returns( true );

            provider.AddConnection( clientId, connection.Object );

            int currentClients = provider.ActiveClients.Count();

            connection.Setup( c => c.IsConnected ).Returns( false );

            int finalClients = provider.ActiveClients.Count();
            currentClients.Should().Be( existingClients + 1 );
            existingClients.Should().Be( finalClients );
        }

        [Test]
        public void when_removing_clients_then_connection_list_decreases()
        {
            ConnectionProvider provider = new ConnectionProvider();

            int initialClients = provider.Connections;

            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == true ) );

            int newClients = provider.Connections;

            provider.RemoveConnection( clientId );

            int finalClients = provider.Connections;
            newClients.Should().Be( initialClients + 1 );
            initialClients.Should().Be( finalClients );
        }

        [Test]
        public void when_adding_existing_client_id_then_existing_client_is_disconnected()
        {
            ConnectionProvider provider = new ConnectionProvider();

            Subject<IPacket> receiver1 = new Subject<IPacket>();
            Mock<IMqttChannel<IPacket>> channel1 = new Mock<IMqttChannel<IPacket>>();

            channel1.Setup( c => c.ReceiverStream ).Returns( receiver1 );
            channel1.Setup( c => c.IsConnected ).Returns( true );

            Subject<IPacket> receiver2 = new Subject<IPacket>();
            Mock<IMqttChannel<IPacket>> channel2 = new Mock<IMqttChannel<IPacket>>();

            channel2.Setup( c => c.ReceiverStream ).Returns( receiver2 );
            channel2.Setup( c => c.IsConnected ).Returns( true );

            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, channel1.Object );
            provider.AddConnection( clientId, channel2.Object );

            channel1.Verify( c => c.Dispose() );
            channel2.Verify( c => c.Dispose(), Times.Never );
        }

        [Test]
        public void when_getting_connection_from_client_then_succeeds()
        {
            ConnectionProvider provider = new ConnectionProvider();
            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == true ) );

            IMqttChannel<IPacket> connection = provider.GetConnection( clientId );

            Assert.NotNull( connection );
        }

        [Test]
        public void when_getting_connection_from_disconnected_client_then_no_connection_is_returned()
        {
            ConnectionProvider provider = new ConnectionProvider();
            string clientId = Guid.NewGuid().ToString();

            provider.AddConnection( clientId, Mock.Of<IMqttChannel<IPacket>>( c => c.IsConnected == false ) );

            IMqttChannel<IPacket> connection = provider.GetConnection( clientId );

            Assert.Null( connection );
        }
    }
}
