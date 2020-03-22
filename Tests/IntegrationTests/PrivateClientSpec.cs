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
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );

            Assert.NotNull( client );
            Assert.True( client.CheckConnection( TestHelper.Monitor ) );
            Assert.False( string.IsNullOrEmpty( client.ClientId ) );
            Assert.True( client.ClientId.StartsWith( "private" ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_topic_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );
            string topicFilter = Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( TestHelper.Monitor, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( client.CheckConnection( TestHelper.Monitor ) );

            await client.UnsubscribeAsync( TestHelper.Monitor, new string[] { topicFilter } );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_system_topic_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );
            string topicFilter = "$SYS/" + Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( TestHelper.Monitor, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( client.CheckConnection( TestHelper.Monitor ) );

            await client.UnsubscribeAsync( TestHelper.Monitor, new string[] { topicFilter } );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_publish_messages_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );
            string topic = Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            var payload = Serializer.Serialize( testMessage );

            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.ExactlyOnce );

            Assert.True( client.CheckConnection( TestHelper.Monitor ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_publish_system_messages_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );
            string topic = "$SYS/" + Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            var payload = Serializer.Serialize( testMessage );

            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( TestHelper.Monitor, topic, payload, MqttQualityOfService.ExactlyOnce );

            Assert.True( client.CheckConnection( TestHelper.Monitor ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_disconnect_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor );
            string clientId = client.ClientId;

            await client.DisconnectAsync( TestHelper.Monitor );

            Assert.False( Server.ActiveClients.Any( c => c == clientId ) );
            Assert.False( client.CheckConnection( TestHelper.Monitor ) );
            Assert.True( string.IsNullOrEmpty( client.ClientId ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_clients_communicate_each_other_then_succeeds()
        {
            IMqttConnectedClient fooClient = await Server.CreateClientAsync( TestHelper.Monitor );
            IMqttConnectedClient barClient = await Server.CreateClientAsync( TestHelper.Monitor );
            string fooTopic = "foo/message";

            await fooClient.SubscribeAsync( TestHelper.Monitor, fooTopic, MqttQualityOfService.ExactlyOnce );

            int messagesReceived = 0;

            fooClient.MessageReceived += ( m, sender, message ) =>
            {
                if( message.Topic == fooTopic )
                {
                    messagesReceived++;
                }
            };

            await barClient.PublishAsync( TestHelper.Monitor, fooTopic, new byte[255], MqttQualityOfService.AtMostOnce );
            await barClient.PublishAsync( TestHelper.Monitor, fooTopic, new byte[10], MqttQualityOfService.AtLeastOnce );
            await barClient.PublishAsync( TestHelper.Monitor, "other/topic", new byte[500], MqttQualityOfService.ExactlyOnce );
            await barClient.PublishAsync( TestHelper.Monitor, fooTopic, new byte[50], MqttQualityOfService.ExactlyOnce );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( fooClient.CheckConnection( TestHelper.Monitor ) );
            Assert.True( barClient.CheckConnection( TestHelper.Monitor ) );
            messagesReceived.Should().Be( 3 );

            fooClient.Dispose();
            barClient.Dispose();
        }

        [Test]
        public async Task when_in_process_client_communicate_with_tcp_client_then_succeeds()
        {
            var mInProcess = new ActivityMonitor();
            using( IMqttConnectedClient inProcessClient = await Server.CreateClientAsync( mInProcess ) )
            {
                (IMqttClient remoteClient, IActivityMonitor mRemote) = await GetClientAsync();
                using( remoteClient )
                {
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

                    Assert.True( inProcessClient.CheckConnection( mInProcess ) );
                    Assert.True( remoteClient.CheckConnection( mRemote ) );
                    fooMessagesReceived.Should().Be( 3 );
                    barMessagesReceived.Should().Be( 3 );
                }
            }
        }
    }
}
