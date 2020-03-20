using CK.Core;
using CK.MQTT;

using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    public class UnsubscribeFlowSpec
    {
        [Test]
        public async Task when_unsubscribing_existing_subscriptions_then_subscriptions_are_deleted_and_ack_is_sent()
        {
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            string clientId = Guid.NewGuid().ToString();
            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            string topic = "foo/bar/test";
            MqttQualityOfService qos = MqttQualityOfService.AtLeastOnce;
            ClientSession session = new ClientSession( clientId, clean: false )
            {
                Subscriptions = new List<ClientSubscription> {
                        new ClientSubscription { ClientId = clientId, MaximumQualityOfService = qos, TopicFilter = topic }
                    }
            };
            ClientSession updatedSession = default;

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );
            sessionRepository.Setup( r => r.Update( It.IsAny<ClientSession>() ) ).Callback<ClientSession>( s => updatedSession = s );

            Unsubscribe unsubscribe = new Unsubscribe( packetId, new string[] { topic } );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            IPacket response = default;

            channel.Setup( c => c.SendAsync( It.IsAny<Mon<IPacket>>() ) )
                .Callback<Mon<IPacket>>( p => response = p.Item )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerUnsubscribeFlow flow = new ServerUnsubscribeFlow( sessionRepository.Object );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, unsubscribe, channel.Object );

            Assert.NotNull( response );
            0.Should().Be( updatedSession.Subscriptions.Count );

            UnsubscribeAck unsubscribeAck = response as UnsubscribeAck;

            Assert.NotNull( unsubscribeAck );
            packetId.Should().Be( unsubscribeAck.PacketId );
        }

        [Test]
        public async Task when_unsubscribing_not_existing_subscriptions_then_ack_is_sent()
        {
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            string clientId = Guid.NewGuid().ToString();
            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            ClientSession session = new ClientSession( clientId, clean: false );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            Unsubscribe unsubscribe = new Unsubscribe( packetId, new string[] { "foo/bar" } );

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            IPacket response = default;

            channel.Setup( c => c.SendAsync( It.IsAny<Mon<IPacket>>() ) )
                .Callback<Mon<IPacket>>( p => response = p.Item )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( TestHelper.Monitor, It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerUnsubscribeFlow flow = new ServerUnsubscribeFlow( sessionRepository.Object );

            await flow.ExecuteAsync( TestHelper.Monitor, clientId, unsubscribe, channel.Object );

            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            Assert.NotNull( response );

            UnsubscribeAck unsubscribeAck = response as UnsubscribeAck;

            Assert.NotNull( unsubscribeAck );
            packetId.Should().Be( unsubscribeAck.PacketId );
        }
    }
}
