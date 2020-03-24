using CK.Core;
using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using IntegrationTests.Messages;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{
    public abstract class PrivateClientSpec : IntegrationContext
    {
        [Test]
        public async Task when_creating_in_process_client_then_it_is_already_connected()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );

            Assert.NotNull( client );
            Assert.True( await client.CheckConnectionAsync( m ) );
            Assert.False( string.IsNullOrEmpty( client.ClientId ) );
            Assert.True( client.ClientId.StartsWith( "private" ) );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_topic_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );
            string topicFilter = Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( await client.CheckConnectionAsync( m ) );

            await client.UnsubscribeAsync( m, new string[] { topicFilter } );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_system_topic_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );
            string topicFilter = "$SYS/" + Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( m, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( await client.CheckConnectionAsync( m ) );

            await client.UnsubscribeAsync( m, new string[] { topicFilter } );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_client_publish_messages_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );
            string topic = Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            var payload = Serializer.Serialize( testMessage );

            await client.PublishAsync( m, topic, payload, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( m, topic, payload, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( m, topic, payload, MqttQualityOfService.ExactlyOnce );

            Assert.True( await client.CheckConnectionAsync( m ) );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_client_publish_system_messages_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );
            string topic = "$SYS/" + Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            var payload = Serializer.Serialize( testMessage );

            await client.PublishAsync( m, topic, payload, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( m, topic, payload, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( m, topic, payload, MqttQualityOfService.ExactlyOnce );

            Assert.True( await client.CheckConnectionAsync( m ) );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_client_disconnect_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient client = await Server.CreateClientAsync( m );
            string clientId = client.ClientId;

            await client.DisconnectAsync( m );

            Assert.False( Server.ActiveClients.Any( c => c == clientId ) );
            Assert.False( await client.CheckConnectionAsync( m ) );
            Assert.True( string.IsNullOrEmpty( client.ClientId ) );

            await client.DisconnectAsync( m );
        }

        [Test]
        public async Task when_in_process_clients_communicate_each_other_then_succeeds()
        {
            var m = new ActivityMonitor();
            IMqttConnectedClient fooClient = await Server.CreateClientAsync( m );
            IMqttConnectedClient barClient = await Server.CreateClientAsync( m );
            string fooTopic = "foo/message";

            await fooClient.SubscribeAsync( m, fooTopic, MqttQualityOfService.ExactlyOnce );

            int messagesReceived = 0;

            fooClient.MessageReceived += ( m, sender, message ) =>
            {
                if( message.Topic == fooTopic )
                {
                    messagesReceived++;
                }
            };

            await barClient.PublishAsync( m, fooTopic, new byte[255], MqttQualityOfService.AtMostOnce );
            await barClient.PublishAsync( m, fooTopic, new byte[10], MqttQualityOfService.AtLeastOnce );
            await barClient.PublishAsync( m, "other/topic", new byte[500], MqttQualityOfService.ExactlyOnce );
            await barClient.PublishAsync( m, fooTopic, new byte[50], MqttQualityOfService.ExactlyOnce );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( await fooClient.CheckConnectionAsync( m ) );
            Assert.True( await barClient.CheckConnectionAsync( m ) );
            messagesReceived.Should().Be( 3 );

            await fooClient.DisconnectAsync(m);
            await barClient.DisconnectAsync(m);
        }

        [Test]
        public async Task when_in_process_client_communicate_with_tcp_client_then_succeeds()
        {
            var mInProcess = new ActivityMonitor();
            IMqttConnectedClient inProcessClient = await Server.CreateClientAsync( mInProcess );
            (IMqttClient remoteClient, IActivityMonitor mRemote) = await GetClientAsync();
            await remoteClient.ConnectAsync( mRemote, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

            string fooTopic = "foo/message";
            string barTopic = "bar/message";

            await inProcessClient.SubscribeAsync( mInProcess, fooTopic, MqttQualityOfService.ExactlyOnce );
            await remoteClient.SubscribeAsync( mRemote, barTopic, MqttQualityOfService.AtLeastOnce );

            int fooMessagesReceived = 0;
            int barMessagesReceived = 0;

            inProcessClient.MessageReceived += ( m, sender, message ) =>
            {
                if( message.Topic == fooTopic )
                {
                    fooMessagesReceived++;
                }
            };
            remoteClient.MessageReceived += ( m, sender, message ) =>
            {
                if( message.Topic == barTopic )
                {
                    barMessagesReceived++;
                }
            };

            await remoteClient.PublishAsync( mRemote, fooTopic, new byte[255], MqttQualityOfService.AtMostOnce );
            await remoteClient.PublishAsync( mRemote, fooTopic, new byte[10], MqttQualityOfService.AtLeastOnce );
            await remoteClient.PublishAsync( mRemote, "other/topic", new byte[500], MqttQualityOfService.ExactlyOnce );
            await remoteClient.PublishAsync( mRemote, fooTopic, new byte[50], MqttQualityOfService.ExactlyOnce );

            await inProcessClient.PublishAsync( mInProcess, barTopic, new byte[255], MqttQualityOfService.AtMostOnce );
            await inProcessClient.PublishAsync( mInProcess, barTopic, new byte[10], MqttQualityOfService.AtLeastOnce );
            await inProcessClient.PublishAsync( mInProcess, "other/topic", new byte[500], MqttQualityOfService.ExactlyOnce );
            await inProcessClient.PublishAsync( mInProcess, barTopic, new byte[50], MqttQualityOfService.ExactlyOnce );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( await inProcessClient.CheckConnectionAsync( mInProcess ) );
            Assert.True( await remoteClient.CheckConnectionAsync( mRemote ) );
            fooMessagesReceived.Should().Be( 3 );
            barMessagesReceived.Should().Be( 3 );
            await remoteClient.DisconnectAsync( mRemote );
            await inProcessClient.DisconnectAsync( mInProcess );
        }
    }
}
