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
            Assert.True( client.IsConnected );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
            Assert.True( client.Id.StartsWith( "private" ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_topic_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor);
            string topicFilter = Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( TestHelper.Monitor, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( client.IsConnected );

            await client.UnsubscribeAsync( TestHelper.Monitor, topicFilter );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_subscribe_to_system_topic_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor);
            string topicFilter = "$SYS/" + Guid.NewGuid().ToString() + "/#";

            await client.SubscribeAsync( TestHelper.Monitor, topicFilter, MqttQualityOfService.AtMostOnce );

            Assert.True( client.IsConnected );

            await client.UnsubscribeAsync( TestHelper.Monitor, topicFilter );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_publish_messages_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor);
            string topic = Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.ExactlyOnce );

            Assert.True( client.IsConnected );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_publish_system_messages_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor);
            string topic = "$SYS/" + Guid.NewGuid().ToString();
            TestMessage testMessage = new TestMessage
            {
                Name = string.Concat( "Message ", Guid.NewGuid().ToString().Substring( 0, 4 ) ),
                Value = new Random().Next()
            };
            MqttApplicationMessage message = new MqttApplicationMessage( topic, Serializer.Serialize( testMessage ) );

            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtMostOnce );
            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.AtLeastOnce );
            await client.PublishAsync( TestHelper.Monitor, message, MqttQualityOfService.ExactlyOnce );

            Assert.True( client.IsConnected );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_client_disconnect_then_succeeds()
        {
            IMqttConnectedClient client = await Server.CreateClientAsync( TestHelper.Monitor);
            string clientId = client.Id;

            await client.DisconnectAsync( TestHelper.Monitor );

            Assert.False( Server.ActiveClients.Any( c => c == clientId ) );
            Assert.False( client.IsConnected );
            Assert.True( string.IsNullOrEmpty( client.Id ) );

            client.Dispose();
        }

        [Test]
        public async Task when_in_process_clients_communicate_each_other_then_succeeds()
        {
            IMqttConnectedClient fooClient = await Server.CreateClientAsync( TestHelper.Monitor);
            IMqttConnectedClient barClient = await Server.CreateClientAsync( TestHelper.Monitor);
            string fooTopic = "foo/message";

            await fooClient.SubscribeAsync( TestHelper.Monitor, fooTopic, MqttQualityOfService.ExactlyOnce );

            int messagesReceived = 0;

            fooClient.MessageStream.Subscribe( message =>
            {
                if( message.Item.Topic == fooTopic )
                {
                    messagesReceived++;
                }
            } );

            await barClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[255] ), MqttQualityOfService.AtMostOnce );
            await barClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[10] ), MqttQualityOfService.AtLeastOnce );
            await barClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( "other/topic", new byte[500] ), MqttQualityOfService.ExactlyOnce );
            await barClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[50] ), MqttQualityOfService.ExactlyOnce );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( fooClient.IsConnected );
            Assert.True( barClient.IsConnected );
            messagesReceived.Should().Be( 3 );

            fooClient.Dispose();
            barClient.Dispose();
        }

        [Test]
        public async Task when_in_process_client_communicate_with_tcp_client_then_succeeds()
        {
            IMqttConnectedClient inProcessClient = await Server.CreateClientAsync( TestHelper.Monitor);
            IMqttClient remoteClient = await GetClientAsync();

            await remoteClient.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

            string fooTopic = "foo/message";
            string barTopic = "bar/message";

            await inProcessClient.SubscribeAsync( TestHelper.Monitor, fooTopic, MqttQualityOfService.ExactlyOnce );
            await remoteClient.SubscribeAsync( TestHelper.Monitor, barTopic, MqttQualityOfService.AtLeastOnce );

            int fooMessagesReceived = 0;
            int barMessagesReceived = 0;

            inProcessClient.MessageStream.Subscribe( message =>
            {
                if( message.Item.Topic == fooTopic )
                {
                    fooMessagesReceived++;
                }
            } );
            remoteClient.MessageStream.Subscribe( message =>
            {
                if( message.Item.Topic == barTopic )
                {
                    barMessagesReceived++;
                }
            } );

            await remoteClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[255] ), MqttQualityOfService.AtMostOnce );
            await remoteClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[10] ), MqttQualityOfService.AtLeastOnce );
            await remoteClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( "other/topic", new byte[500] ), MqttQualityOfService.ExactlyOnce );
            await remoteClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( fooTopic, new byte[50] ), MqttQualityOfService.ExactlyOnce );

            await inProcessClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( barTopic, new byte[255] ), MqttQualityOfService.AtMostOnce );
            await inProcessClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( barTopic, new byte[10] ), MqttQualityOfService.AtLeastOnce );
            await inProcessClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( "other/topic", new byte[500] ), MqttQualityOfService.ExactlyOnce );
            await inProcessClient.PublishAsync( TestHelper.Monitor, new MqttApplicationMessage( barTopic, new byte[50] ), MqttQualityOfService.ExactlyOnce );

            await Task.Delay( TimeSpan.FromMilliseconds( 1000 ) );

            Assert.True( inProcessClient.IsConnected );
            Assert.True( remoteClient.IsConnected );
            fooMessagesReceived.Should().Be( 3 );
            barMessagesReceived.Should().Be( 3 );

            inProcessClient.Dispose();
            remoteClient.Dispose();
        }
    }
}
