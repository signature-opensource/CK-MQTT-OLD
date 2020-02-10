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
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Flows
{
    public class ConnectFlowSpec
    {
        [Test]
        public async Task when_sending_connect_then_session_is_created_and_ack_is_sent()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();
            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            sessionRepository.Verify( r => r.Create( It.Is<ClientSession>( s => s.Id == clientId && s.Clean == true ) ) );
            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            willRepository.Verify( r => r.Create( It.IsAny<ConnectionWill>() ), Times.Never );

            Assert.NotNull( sentPacket );

            ConnectAck connectAck = sentPacket as ConnectAck;

            Assert.NotNull( connectAck );
            connectAck.Type.Should().Be( MqttPacketType.ConnectAck );
            connectAck.Status.Should().Be( MqttConnectionStatus.Accepted );
            connectAck.SessionPresent.Should().BeFalse();
        }

        [Test]
        public async Task when_sending_connect_with_existing_session_and_without_clean_session_then_session_is_not_deleted_and_ack_is_sent_with_session_present()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession existingSession = new ClientSession( clientId, clean: false );

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( existingSession );

            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            Connect connect = new Connect( clientId, cleanSession: false );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            sessionRepository.Verify( r => r.Create( It.IsAny<ClientSession>() ), Times.Never );
            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            willRepository.Verify( r => r.Create( It.IsAny<ConnectionWill>() ), Times.Never );

            ConnectAck connectAck = sentPacket as ConnectAck;

            Assert.NotNull( connectAck );
            connectAck.Type.Should().Be( MqttPacketType.ConnectAck );
            connectAck.Status.Should().Be( MqttConnectionStatus.Accepted );

            connectAck.SessionPresent.Should().BeTrue();
        }

        [Test]
        public async Task when_sending_connect_with_existing_session_and_clean_session_then_session_is_deleted_and_ack_is_sent_with_session_present()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession existingSession = new ClientSession( clientId, clean: true );

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( existingSession );

            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            Connect connect = new Connect( clientId, cleanSession: true );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            ConnectAck connectAck = sentPacket as ConnectAck;

            sessionRepository.Verify( r => r.Delete( It.Is<string>( s => s == existingSession.Id ) ) );
            sessionRepository.Verify( r => r.Create( It.Is<ClientSession>( s => s.Clean == true ) ) );
            willRepository.Verify( r => r.Create( It.IsAny<ConnectionWill>() ), Times.Never );

            Assert.NotNull( connectAck );
            connectAck.Type.Should().Be( MqttPacketType.ConnectAck );
            connectAck.Status.Should().Be( MqttConnectionStatus.Accepted );
            Assert.False( connectAck.SessionPresent );
        }

        [Test]
        public async Task when_sending_connect_without_existing_session_and_without_clean_session_then_ack_is_sent_with_no_session_present()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            string clientId = Guid.NewGuid().ToString();

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( default( ClientSession ) );

            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            Connect connect = new Connect( clientId, cleanSession: false );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            ConnectAck connectAck = sentPacket as ConnectAck;

            Assert.False( connectAck.SessionPresent );
        }

        [Test]
        public async Task when_sending_connect_with_will_then_will_is_created_and_ack_is_sent()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true );

            FooWillMessage willMessage = new FooWillMessage { Message = "Foo Will Message" };
            MqttLastWill will = new MqttLastWill( "foo/bar", MqttQualityOfService.AtLeastOnce, retain: true, payload: willMessage.GetPayload() );

            connect.Will = will;

            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            ConnectAck connectAck = sentPacket as ConnectAck;

            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            sessionRepository.Verify( r => r.Create( It.Is<ClientSession>( s => s.Id == clientId && s.Clean == true ) ) );
            willRepository.Verify( r => r.Create( It.Is<ConnectionWill>( w => w.Id == clientId && w.Will == will ) ) );

            Assert.NotNull( connectAck );
            connectAck.Type.Should().Be( MqttPacketType.ConnectAck );
            connectAck.Status.Should().Be( MqttConnectionStatus.Accepted );
            Assert.False( connectAck.SessionPresent );
        }

        [Test]
        public void when_sending_connect_with_invalid_user_credentials_then_connection_exception_is_thrown()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == false );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();
            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            string clientId = Guid.NewGuid().ToString();
            Connect connect = new Connect( clientId, cleanSession: true );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            AggregateException aggregateEx = Assert.Throws<AggregateException>( () => flow.ExecuteAsync( clientId, connect, channel.Object ).Wait() );

            Assert.NotNull( aggregateEx.InnerException );
            Assert.True( aggregateEx.InnerException is MqttConnectionException );
            ((MqttConnectionException)aggregateEx.InnerException).ReturnCode.Should()
                .Be( MqttConnectionStatus.BadUserNameOrPassword );
        }

        [Test]
        public async Task when_sending_connect_with_existing_session_and_without_clean_session_then_pending_messages_and_acks_are_sent()
        {
            IMqttAuthenticationProvider authenticationProvider = Mock.Of<IMqttAuthenticationProvider>( p => p.Authenticate( It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>() ) == true );
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();
            Mock<IRepository<ConnectionWill>> willRepository = new Mock<IRepository<ConnectionWill>>();

            string clientId = Guid.NewGuid().ToString();
            ClientSession existingSession = new ClientSession( clientId, clean: false );

            string topic = "foo/bar";
            byte[] payload = new byte[10];
            MqttQualityOfService qos = MqttQualityOfService.ExactlyOnce;
            ushort packetId = 10;

            existingSession.PendingMessages = new List<PendingMessage>
            {
                new PendingMessage {
                    Status = PendingMessageStatus.PendingToSend,
                    Topic = topic,
                    QualityOfService = qos,
                    Retain = false,
                    Duplicated = false,
                    PacketId = packetId,
                    Payload = payload
                },
                new PendingMessage {
                    Status = PendingMessageStatus.PendingToAcknowledge,
                    Topic = topic,
                    QualityOfService = qos,
                    Retain = false,
                    Duplicated = false,
                    PacketId = packetId,
                    Payload = payload
                }
            };

            existingSession.PendingAcknowledgements = new List<PendingAcknowledgement>
            {
                new PendingAcknowledgement { Type = MqttPacketType.PublishReceived, PacketId = packetId }
            };

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( existingSession );

            Mock<IPublishSenderFlow> senderFlow = new Mock<IPublishSenderFlow>();

            senderFlow
                .Setup( f => f.SendPublishAsync( It.IsAny<string>(), It.IsAny<Publish>(), It.IsAny<IMqttChannel<IPacket>>(), It.IsAny<PendingMessageStatus>() ) )
                .Callback<string, Publish, IMqttChannel<IPacket>, PendingMessageStatus>( async ( id, pub, ch, stat ) =>
                 {
                     await ch.SendAsync( pub );
                 } )
                .Returns( Task.Delay( 0 ) );

            senderFlow
                .Setup( f => f.SendAckAsync( It.IsAny<string>(), It.IsAny<IFlowPacket>(), It.IsAny<IMqttChannel<IPacket>>(), It.IsAny<PendingMessageStatus>() ) )
                .Callback<string, IFlowPacket, IMqttChannel<IPacket>, PendingMessageStatus>( async ( id, pack, ch, stat ) =>
                 {
                     await ch.SendAsync( pack );
                 } )
                .Returns( Task.Delay( 0 ) );

            Connect connect = new Connect( clientId, cleanSession: false );
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket firstPacket = default;
            List<IPacket> nextPackets = new List<IPacket>();

            channel
                .Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet =>
                 {
                     if( firstPacket == default( IPacket ) )
                     {
                         firstPacket = packet;
                     }
                     else
                     {
                         nextPackets.Add( packet );
                     }
                 } )
                .Returns( Task.Delay( 0 ) );

            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();

            connectionProvider
                .Setup( p => p.GetConnection( It.Is<string>( c => c == clientId ) ) )
                .Returns( channel.Object );

            ServerConnectFlow flow = new ServerConnectFlow( authenticationProvider, sessionRepository.Object, willRepository.Object, senderFlow.Object );

            await flow.ExecuteAsync( clientId, connect, channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            sessionRepository.Verify( r => r.Create( It.IsAny<ClientSession>() ), Times.Never );
            sessionRepository.Verify( r => r.Delete( It.IsAny<string>() ), Times.Never );
            sessionRepository.Verify( r => r.Update( It.IsAny<ClientSession>() ), Times.Once );
            willRepository.Verify( r => r.Create( It.IsAny<ConnectionWill>() ), Times.Never );
            senderFlow.Verify( f => f.SendPublishAsync( It.Is<string>( x => x == existingSession.Id ),
                 It.Is<Publish>( x => x.Topic == topic && x.QualityOfService == qos && x.PacketId == packetId ),
                 It.IsAny<IMqttChannel<IPacket>>(),
                 It.IsAny<PendingMessageStatus>() ), Times.Exactly( 2 ) );
            senderFlow.Verify( f => f.SendAckAsync( It.Is<string>( x => x == existingSession.Id ),
                 It.Is<IFlowPacket>( x => x.Type == MqttPacketType.PublishReceived && x.PacketId == packetId ),
                 It.IsAny<IMqttChannel<IPacket>>(),
                 It.Is<PendingMessageStatus>( x => x == PendingMessageStatus.PendingToAcknowledge ) ), Times.Once );

            ConnectAck connectAck = firstPacket as ConnectAck;

            Assert.True( connectAck != null, "The first packet sent by the Server must be a CONNACK" );
            connectAck.Type.Should().Be( MqttPacketType.ConnectAck );
            connectAck.Status.Should().Be( MqttConnectionStatus.Accepted );

            Assert.True( connectAck.SessionPresent );
            nextPackets.Count.Should().Be( 3 );

            Assert.False( nextPackets.Any( x => x is ConnectAck ) );
        }
    }
}
