using Moq;
using System;
using System.Linq;
using CK.MQTT;
using CK.MQTT.Sdk.Packets;
using System.Reactive.Subjects;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;

namespace Tests
{
	public class ConnectionProviderSpec
	{
        [Test]
        public void when_registering_private_client_then_private_client_list_increases()
        {
            var provider = new ConnectionProvider ();

            var existingPrivateClients = provider.PrivateClients.Count ();
            var clientId = Guid.NewGuid ().ToString ();

            provider.RegisterPrivateClient (clientId);
            provider.PrivateClients.Should().HaveCount( existingPrivateClients + 1 );
        }

        [Test]
        public void when_registering_existing_private_client_then_fails()
        {
            var provider = new ConnectionProvider ();

            var clientId = Guid.NewGuid().ToString ();

            provider.RegisterPrivateClient (clientId);

            var ex = Assert.Throws<MqttServerException> (() => provider.RegisterPrivateClient (clientId));

            Assert.NotNull (ex);
        }

        [Test]
        public void when_removing_private_connection_then_private_client_list_decreases()
        {
            var provider = new ConnectionProvider ();

            var clientId = Guid.NewGuid ().ToString ();

            provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));
            provider.RegisterPrivateClient (clientId);

            var previousPrivateClients = provider.PrivateClients.Count ();

            provider.RemoveConnection (clientId);

            var currentPrivateClients = provider.PrivateClients.Count ();
            currentPrivateClients.Should().Be( previousPrivateClients - 1 );
        }

        [Test]
		public void when_adding_new_client_then_connection_list_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.Connections;
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));
            provider.Connections.Should().Be( existingClients + 1 );
		}

		[Test]
		public void when_adding_disconnected_client_then_active_clients_list_does_not_increases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == false));

            provider.ActiveClients.Should().HaveCount( existingClients );
		}

		[Test]
		public void when_adding_new_client_and_disconnect_it_then_active_clients_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var existingClients = provider.ActiveClients.Count();
			var clientId = Guid.NewGuid ().ToString ();

			var connection = new Mock<IMqttChannel<IPacket>> ();

			connection.Setup(c => c.IsConnected).Returns(true);

			provider.AddConnection (clientId, connection.Object);

			var currentClients = provider.ActiveClients.Count();

			connection.Setup (c => c.IsConnected).Returns (false);

			var finalClients = provider.ActiveClients.Count();
            currentClients.Should().Be( existingClients + 1 );
            existingClients.Should().Be( finalClients );
		}

		[Test]
		public void when_removing_clients_then_connection_list_decreases()
		{
			var provider = new ConnectionProvider ();

			var initialClients = provider.Connections;

			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));

			var newClients = provider.Connections;

			provider.RemoveConnection (clientId);

			var finalClients = provider.Connections;
            newClients.Should().Be( initialClients + 1 );
            initialClients.Should().Be( finalClients );
		}

		[Test]
		public void when_adding_existing_client_id_then_existing_client_is_disconnected()
		{
			var provider = new ConnectionProvider ();

			var receiver1 = new Subject<IPacket> ();
			var channel1 = new Mock<IMqttChannel<IPacket>> ();

			channel1.Setup (c => c.ReceiverStream).Returns (receiver1);
			channel1.Setup (c => c.IsConnected).Returns (true);

			var receiver2 = new Subject<IPacket> ();
			var channel2 = new Mock<IMqttChannel<IPacket>> ();

			channel2.Setup (c => c.ReceiverStream).Returns (receiver2);
			channel2.Setup (c => c.IsConnected).Returns (true);

			var clientId = Guid.NewGuid().ToString();

			provider.AddConnection (clientId, channel1.Object);
			provider.AddConnection (clientId, channel2.Object);

			channel1.Verify (c => c.Dispose ());
			channel2.Verify(c => c.Dispose(), Times.Never);
		}

		[Test]
		public void when_getting_connection_from_client_then_succeeds()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == true));

			var connection = provider.GetConnection (clientId);

			Assert.NotNull (connection);
		}

		[Test]
		public void when_getting_connection_from_disconnected_client_then_no_connection_is_returned()
		{
			var provider = new ConnectionProvider ();
			var clientId = Guid.NewGuid ().ToString ();

			provider.AddConnection (clientId, Mock.Of<IMqttChannel<IPacket>> (c => c.IsConnected == false));

			var connection = provider.GetConnection (clientId);

			Assert.Null (connection);
		}
	}
}
