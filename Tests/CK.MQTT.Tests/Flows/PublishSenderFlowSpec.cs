using CK.MQTT;
using CK.MQTT.Client.Abstractions;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
    public class PublishSenderFlowSpec
    {
        [Test]
        public void when_sending_publish_with_qos1_and_publish_ack_is_not_received_then_publish_is_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 1 && c.MaximumQualityOfService == MqttQualityOfService.AtLeastOnce );
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            PublishSenderFlow flow = new PublishSenderFlow( sessionRepository.Object, configuration );

            string topic = "foo/bar";
            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.AtLeastOnce, retain: false, duplicated: false, packetId: packetId );

            publish.Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" );

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );
            channel.Setup( c => c.SenderStream ).Returns( sender );
            channel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => sender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );

            connectionProvider.Setup( m => m.GetConnection( It.IsAny<string>() ) ).Returns( channel.Object );

            ManualResetEventSlim retrySignal = new ManualResetEventSlim( initialState: false );
            int retries = 0;

            sender.Subscribe( p =>
            {
                if( p.Item is Publish )
                {
                    retries++;
                }

                if( retries > 1 )
                {
                    retrySignal.Set();
                }
            } );

            Task flowTask = flow.SendPublishAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            bool retried = retrySignal.Wait( 2000 );

            Assert.True( retried );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is Publish &&
                ((Publish)p.Item).Topic == topic &&
                ((Publish)p.Item).QualityOfService == MqttQualityOfService.AtLeastOnce &&
                ((Publish)p.Item).PacketId == packetId ) ), Times.AtLeast( 2 ) );
        }

        [Test]
        public void when_sending_publish_with_qos2_and_publish_received_is_not_received_then_publish_is_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 1 && c.MaximumQualityOfService == MqttQualityOfService.ExactlyOnce );
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            PublishSenderFlow flow = new PublishSenderFlow( sessionRepository.Object, configuration );

            string topic = "foo/bar";
            ushort? packetId = (ushort?)new Random().Next( 0, ushort.MaxValue );
            Publish publish = new Publish( topic, MqttQualityOfService.ExactlyOnce, retain: false, duplicated: false, packetId: packetId );

            publish.Payload = Encoding.UTF8.GetBytes( "Publish Receiver Flow Test" );

            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );
            channel.Setup( c => c.SenderStream ).Returns( sender );
            channel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => sender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );

            connectionProvider.Setup( m => m.GetConnection( It.IsAny<string>() ) ).Returns( channel.Object );

            ManualResetEventSlim retrySignal = new ManualResetEventSlim( initialState: false );
            int retries = 0;

            sender.Subscribe( p =>
            {
                if( p.Item is Publish )
                {
                    retries++;
                }

                if( retries > 1 )
                {
                    retrySignal.Set();
                }
            } );

            Task flowTask = flow.SendPublishAsync( TestHelper.Monitor, clientId, publish, channel.Object );

            bool retried = retrySignal.Wait( 2000 );

            Assert.True( retried );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is Publish &&
                ((Publish)p.Item).Topic == topic &&
                ((Publish)p.Item).QualityOfService == MqttQualityOfService.ExactlyOnce &&
                ((Publish)p.Item).PacketId == packetId ) ), Times.AtLeast( 2 ) );
        }

        [Test]
        public void when_sending_publish_received_then_publish_release_is_sent()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 10 );
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            PublishSenderFlow flow = new PublishSenderFlow( sessionRepository.Object, configuration );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            PublishReceived publishReceived = new PublishReceived( packetId );
            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );
            channel.Setup( c => c.SenderStream ).Returns( sender );
            channel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => sender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );

            connectionProvider.Setup( m => m.GetConnection( It.Is<string>( s => s == clientId ) ) ).Returns( channel.Object );

            ManualResetEventSlim ackSentSignal = new ManualResetEventSlim( initialState: false );

            sender.Subscribe( p =>
            {
                if( p.Item is PublishRelease )
                {
                    ackSentSignal.Set();
                }
            } );

            Task flowTask = flow.ExecuteAsync( TestHelper.Monitor, clientId, publishReceived, channel.Object );

            bool ackSent = ackSentSignal.Wait( 2000 );

            Assert.True( ackSent );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishRelease
                && (p.Item as PublishRelease).PacketId == packetId ) ), Times.AtLeastOnce );
        }

        [Test]
        public void when_sending_publish_received_and_no_complete_is_sent_after_receiving_publish_release_then_publish_release_is_re_transmitted()
        {
            string clientId = Guid.NewGuid().ToString();

            MqttConfiguration configuration = Mock.Of<MqttConfiguration>( c => c.WaitTimeoutSecs == 1 );
            Mock<IConnectionProvider> connectionProvider = new Mock<IConnectionProvider>();
            Mock<IRepository<ClientSession>> sessionRepository = new Mock<IRepository<ClientSession>>();

            sessionRepository
                .Setup( r => r.Read( It.IsAny<string>() ) )
                .Returns( new ClientSession( clientId )
                {
                    PendingMessages = new List<PendingMessage> { new PendingMessage() }
                } );

            PublishSenderFlow flow = new PublishSenderFlow( sessionRepository.Object, configuration );

            ushort packetId = (ushort)new Random().Next( 0, ushort.MaxValue );
            PublishReceived publishReceived = new PublishReceived( packetId );
            Subject<Monitored<IPacket>> receiver = new Subject<Monitored<IPacket>>();
            Subject<Monitored<IPacket>> sender = new Subject<Monitored<IPacket>>();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();

            channel.Setup( c => c.IsConnected ).Returns( true );
            channel.Setup( c => c.ReceiverStream ).Returns( receiver );
            channel.Setup( c => c.SenderStream ).Returns( sender );
            channel.Setup( c => c.SendAsync( It.IsAny<Monitored<IPacket>>() ) )
                .Callback<IPacket>( packet => sender.OnNext( new Monitored<IPacket>( TestHelper.Monitor, packet ) ) )
                .Returns( Task.Delay( 0 ) );

            connectionProvider.Setup( m => m.GetConnection( It.Is<string>( s => s == clientId ) ) ).Returns( channel.Object );

            ManualResetEventSlim ackSentSignal = new ManualResetEventSlim( initialState: false );

            sender.Subscribe( p =>
            {
                if( p.Item is PublishRelease )
                {
                    ackSentSignal.Set();
                }
            } );

            Task flowTask = flow.ExecuteAsync( TestHelper.Monitor, clientId, publishReceived, channel.Object );

            bool ackSent = ackSentSignal.Wait( 2000 );

            Assert.True( ackSent );
            channel.Verify( c => c.SendAsync( It.Is<Monitored<IPacket>>( p => p.Item is PublishRelease
                && (p.Item as PublishRelease).PacketId == packetId ) ), Times.AtLeast( 1 ) );
        }
    }
}
