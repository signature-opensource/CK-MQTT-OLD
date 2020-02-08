using Moq;
using System;
using System.Collections.Generic;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    public class UnsubscribeFlowSpec
    {
        [Test]
        public async Task when_unsubscribing_existing_subscriptions_then_subscriptions_are_deleted_and_ack_is_sent()
        {
            var sessionRepository = new Mock<IRepository<ClientSession>>();
            var clientId = Guid.NewGuid().ToString();
            var packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            var topic = "foo/bar/test";
            var qos = MqttQualityOfService.AtLeastOnce;
            var session = new ClientSession( clientId, clean: false )
            {
                Subscriptions = new List<ClientSubscription> {
                        new ClientSubscription { ClientId = clientId, MaximumQualityOfService = qos, TopicFilter = topic }
                    }
            };
            var updatedSession = default( ClientSession );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );
            sessionRepository.Setup( r => r.Update( It.IsAny<ClientSession>() ) ).Callback<ClientSession>( s => updatedSession = s );

            var unsubscribe = new Unsubscribe( packetId, topic );

            var channel = new Mock<IMqttChannel<IPacket>>();

            var response = default( IPacket );

            channel.Setup( c => c.SendAsync(TestHelper.Monitor, It.IsAny<IPacket>()))
                .Callback<IPacket>( p => response = p )
                .Returns( Task.Delay( 0 ) );

            var connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            var flow = new ServerUnsubscribeFlow( sessionRepository.Object );

            await flow.ExecuteAsync( clientId, unsubscribe, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            Assert.NotNull( response );
            0.Should().Be( updatedSession.Subscriptions.Count );

            var unsubscribeAck = response as UnsubscribeAck;

            Assert.NotNull( unsubscribeAck );
            packetId.Should().Be( unsubscribeAck.PacketId );
        }

        [Test]
        public async Task when_unsubscribing_not_existing_subscriptions_then_ack_is_sent()
        {
            var sessionRepository = new Mock<IRepository<ClientSession>>();
            var clientId = Guid.NewGuid().ToString();
            var packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            var session = new ClientSession( clientId, clean: false );

            sessionRepository.Setup( r => r.Read( It.IsAny<string>() ) ).Returns( session );

            var unsubscribe = new Unsubscribe( packetId, "foo/bar" );

            var channel = new Mock<IMqttChannel<IPacket>>();

            var response = default( IPacket );

            channel.Setup( c => c.SendAsync(TestHelper.Monitor, It.IsAny<IPacket>()))
                .Callback<IPacket>( p => response = p )
                .Returns( Task.Delay( 0 ) );

            var connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            var flow = new ServerUnsubscribeFlow( sessionRepository.Object );

            await flow.ExecuteAsync( clientId, unsubscribe, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            Assert.NotNull( response );

            var unsubscribeAck = response as UnsubscribeAck;

            Assert.NotNull( unsubscribeAck );
            packetId.Should().Be( unsubscribeAck.PacketId );
        }
    }
}
