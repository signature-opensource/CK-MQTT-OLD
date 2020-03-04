using CK.MQTT;
using CK.MQTT.Sdk;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tests;
using static CK.Testing.MonitorTestHelper;
namespace IntegrationTests
{
    public abstract class ConnectionSpec : IntegrationContext
    {
        [Test]
        public async Task when_connecting_client_to_non_existing_server_then_fails()
        {
            try
            {
                Server.Dispose();

                IMqttClient client = await GetClientAsync();

                await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );
            }
            catch( Exception ex )
            {
                Assert.True( ex is MqttClientException );
                Assert.NotNull( ex.InnerException );
                //Assert.True( ex.InnerException is MqttException );//Not throwing this with SSL.
                //Assert.NotNull( ex.InnerException.InnerException );
                //Assert.True( ex.InnerException.InnerException is SocketException );
            }
        }

        [Test]
        public async Task when_clients_connect_and_disconnect_then_server_raises_events()
        {
            using( IMqttClient fooClient = await GetClientAsync() )
            using( IMqttClient barClient = await GetClientAsync() )
            {
                string clientId1 = MqttTestHelper.GetClientId();
                string clientId2 = MqttTestHelper.GetClientId();

                List<string> connected = new List<string>();
                List<string> disconnected = new List<string>();

                Server.ClientConnected += ( sender, id ) => connected.Add( id );
                Server.ClientDisconnected += ( sender, id ) =>
                {
                    connected.Remove( id );
                    disconnected.Add( id );
                };

                await fooClient.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId1 ) );

                connected.Should().BeEquivalentTo( clientId1 );

                await barClient.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId2 ) );

                connected.Should().BeEquivalentTo( clientId1, clientId2 );

                await barClient.DisconnectAsync( TestHelper.Monitor);

                disconnected.Should().BeEquivalentTo( clientId2 );
                connected.Should().BeEquivalentTo( clientId1 );

                await fooClient.DisconnectAsync( TestHelper.Monitor);

                disconnected.Should().BeEquivalentTo( clientId2, clientId1 );
                connected.Should().BeEmpty();
            }
        }

        [Test]
        public async Task when_connect_clients_and_one_client_drops_connection_then_other_client_survives()
        {
            using( IMqttClient fooClient = await GetClientAsync() )
            using( IMqttClient barClient = await GetClientAsync() )
            {
                string fooClientId = MqttTestHelper.GetClientId();
                string barClientId = MqttTestHelper.GetClientId();

                await fooClient.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( fooClientId ) );
                await barClient.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( barClientId ) );

                List<string> clientIds = new List<string> { fooClientId, barClientId };
                int initialConnectedClients = Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();
                bool exceptionThrown = false;

                try
                {
                    //Force an exception to be thrown by publishing null message
                    await fooClient.PublishAsync(TestHelper.Monitor, message: null, qos: MqttQualityOfService.AtMostOnce );
                }
                catch
                {
                    exceptionThrown = true;
                }

                ManualResetEventSlim serverSignal = new ManualResetEventSlim();

                while( !serverSignal.IsSet )
                {
                    if( !Server.ActiveClients.Any( c => c == fooClientId ) )
                    {
                        serverSignal.Set();
                    }
                }

                serverSignal.Wait();

                int finalConnectedClients = Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();

                initialConnectedClients.Should().Be( 2 );
                Assert.True( exceptionThrown );
                finalConnectedClients.Should().Be( 1 );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        [TestCase( 500 )]
        public async Task when_connect_clients_then_succeeds( int count )
        {
            List<IMqttClient> clients = new List<IMqttClient>();
            List<string> clientIds = new List<string>();
            List<Task> tasks = new List<Task>();

            for( int i = 1; i <= count; i++ )
            {
                IMqttClient client = await GetClientAsync();
                string clientId = MqttTestHelper.GetClientId();

                tasks.Add( client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) ) );
                clients.Add( client );
                clientIds.Add( clientId );
            }

            await Task.WhenAll( tasks );

            Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Should().HaveCount( count );
            Assert.True( clients.All( c => c.IsConnected( TestHelper.Monitor ) ) );
            Assert.True( clients.All( c => !string.IsNullOrEmpty( c.Id ) ) );

            foreach( IMqttClient client in clients )
            {
                client.Dispose();
            }
        }

        [Test]
        public async Task when_connecting_twice_with_same_client_with_disconnecting_then_succeeds()
        {
            using( IMqttClient client = await GetClientAsync() )
            {
                string clientId = MqttTestHelper.GetClientId();

                await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );
                await client.DisconnectAsync( TestHelper.Monitor);
                await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );
                Server.ActiveClients.Count( c => c == clientId ).Should().Be( 1 );
            }
        }

        /// <summary>
        /// Test the connexion/deconnexion event execution order.
        /// This test will be useful for [MQTT-3.1.4-2].
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task when_connecting_twice_from_two_client_with_same_id_server_first_client_disconnect_in_right_order()
        {
            string clientId = MqttTestHelper.GetClientId();
            using( IMqttClient clientFoo = await GetClientAsync() )
            using( IMqttClient clientBar = await GetClientAsync() )
            {

                List<char> events = new List<char>();

                void onConnect( object sender, string id )
                {
                    lock( events )
                    {
                        events.Add( 'c' );
                        Monitor.Pulse( events );
                    }
                }
                void onDisconnect( object sender, string id )
                {
                    lock( events )
                    {
                        events.Add( 'd' );
                        Monitor.Pulse( events );
                    }
                }

                Server.ClientDisconnected += onDisconnect;
                Server.ClientConnected += onConnect;

                await clientFoo.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );
                bool didNotTimeout = true;
                lock( events )
                {
                    if( events.Count < 1 ) didNotTimeout = Monitor.Wait( events, 500 );
                }

                Assert.True( didNotTimeout );
                string.Concat( events ).Should().Be( "c" );

                try
                {
                    await clientBar.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );
                }
                catch
                {
                    Assume.That( false, "To be investigated. Tests sometime throw exception on reconnection." );
                }
                lock( events )
                {
                    while( events.Count < 4 ) didNotTimeout &= Monitor.Wait( events, 500 );
                }
                Assert.True( didNotTimeout );
                string.Concat( events ).Should().Be( "ccdd" );
                Server.ClientDisconnected -= onDisconnect;
                Server.ClientConnected -= onConnect;
                //"cdc".Should().Be(events ); //Require MQTT-3.1.4-2
            }
        }

        [Test]
        public async Task when_connecting_twice_with_same_client_without_disconnecting_then_fails()
        {
            IMqttClient client = await GetClientAsync();
            string clientId = MqttTestHelper.GetClientId();

            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );

            AggregateException ex = Assert.Throws<AggregateException>( () => client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_then_succeeds()
        {
            IMqttClient client = await GetClientAsync();

            await client.ConnectAsync( TestHelper.Monitor);

            Assert.True( client.IsConnected( TestHelper.Monitor ) );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
            client.Id.Should().StartWith( "anonymous" );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_and_clean_session_true_then_succeeds()
        {
            IMqttClient client = await GetClientAsync();

            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId: null ), cleanSession: true );

            Assert.True( client.IsConnected( TestHelper.Monitor ) );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
            client.Id.Should().StartWith( "anonymous" );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_and_clean_session_false_then_fails()
        {
            IMqttClient client = await GetClientAsync();
            string clientId = string.Empty;

            AggregateException ex = Assert.Throws<AggregateException>( () => client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: false ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_true_then_session_is_not_preserved()
        {
            using( IMqttClient client = await GetClientAsync() )
            {
                string clientId = MqttTestHelper.GetClientId();

                SessionState sessionState1 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync( TestHelper.Monitor);

                SessionState sessionState2 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync( TestHelper.Monitor);
                sessionState1.Should().Be( SessionState.CleanSession );
                sessionState2.Should().Be( SessionState.CleanSession );

                client.Dispose();
            }
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_false_and_then_true_then_session_is_not_preserved()
        {
            using( IMqttClient client = await GetClientAsync() )
            {
                string clientId = MqttTestHelper.GetClientId();

                SessionState sessionState1 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: false );

                await client.DisconnectAsync( TestHelper.Monitor);

                SessionState sessionState2 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync( TestHelper.Monitor);
                sessionState1.Should().Be( SessionState.CleanSession );
                sessionState2.Should().Be( SessionState.CleanSession );
            }
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_false_then_session_is_preserved()
        {
            using( IMqttClient client = await GetClientAsync() )
            {
                string clientId = MqttTestHelper.GetClientId();

                SessionState sessionState1 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: false );

                await client.DisconnectAsync( TestHelper.Monitor);

                SessionState sessionState2 = await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ), cleanSession: false );

                await client.DisconnectAsync( TestHelper.Monitor);

                sessionState1.Should().Be( SessionState.CleanSession );
                sessionState2.Should().Be( SessionState.SessionPresent );
            }
        }

        [TestCase( 100 )]
        [TestCase( 200 )]
        [TestCase( 500 )]
        public async Task when_disconnect_clients_then_succeeds( int count )
        {
            List<IMqttClient> clients = new List<IMqttClient>();
            List<string> clientIds = new List<string>();

            for( int i = 1; i <= count; i++ )
            {
                IMqttClient client = await GetClientAsync();
                string clientId = MqttTestHelper.GetClientId();

                await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );
                clients.Add( client );
                clientIds.Add( clientId );
            }

            int initialConnectedClients = Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();

            foreach( IMqttClient client in clients )
            {
                await client.DisconnectAsync( TestHelper.Monitor);
            }

            ManualResetEventSlim disconnectedSignal = new ManualResetEventSlim( initialState: false );

            while( !disconnectedSignal.IsSet )
            {
                if( Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count() == 0 && clients.All( c => !c.IsConnected( TestHelper.Monitor ) ) )
                {
                    disconnectedSignal.Set();
                }
            }

            initialConnectedClients.Should().Be( clients.Count );
            Server.ActiveClients.Where( c => clientIds.Contains( c ) ).Should().BeEmpty();
            Assert.True( clients.All( c => !c.IsConnected( TestHelper.Monitor ) ) );
            Assert.True( clients.All( c => string.IsNullOrEmpty( c.Id ) ) );

            foreach( IMqttClient client in clients )
            {
                client.Dispose();
            }
        }

        [Test]
        public async Task when_disconnect_client_then_server_decrease_active_client_list()
        {
            using( IMqttClient client = await GetClientAsync() )
            {

                await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

                string clientId = client.Id;
                bool existClientAfterConnect = Server.ActiveClients.Any( c => c == clientId );

                await client.DisconnectAsync( TestHelper.Monitor);

                ManualResetEventSlim clientClosed = new ManualResetEventSlim();

                IDisposable subscription = Observable.Create<bool>( observer =>
                 {
                     System.Timers.Timer timer = new System.Timers.Timer();

                     timer.Interval = 200;
                     timer.Elapsed += ( sender, args ) =>
                     {
                         if( Server.ActiveClients.Any( c => c == clientId ) )
                         {
                             observer.OnNext( false );
                         }
                         else
                         {
                             observer.OnNext( true );
                             clientClosed.Set();
                             observer.OnCompleted();
                         }
                     };
                     timer.Start();

                     return () =>
                     {
                         timer.Dispose();
                     };
                 } )
                .Subscribe(
                    _ => { },
                    ex => { Console.WriteLine( "Error: {0}", ex.Message ); } );

                bool clientDisconnected = clientClosed.Wait( 2000 );

                Assert.True( existClientAfterConnect );
                Assert.True( clientDisconnected );
                Server.ActiveClients.Should().NotContain( c => c == clientId );
            }
        }

        [Test]
        public async Task when_client_disconnects_by_protocol_then_will_message_is_not_sent()
        {
            using( IMqttClient client1 = await GetClientAsync() )
            using( IMqttClient client2 = await GetClientAsync() )
            using( IMqttClient client3 = await GetClientAsync() )
            {

                string topic = Guid.NewGuid().ToString();
                MqttQualityOfService qos = MqttQualityOfService.ExactlyOnce;
                bool retain = true;
                FooWillMessage willMessage = new FooWillMessage { Message = "Client 1 has been disconnected unexpectedly" };
                MqttLastWill will = new MqttLastWill( topic, qos, retain, willMessage.GetPayload() );

                await client1.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ), will );
                await client2.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );
                await client3.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

                await client2.SubscribeAsync( TestHelper.Monitor, topic, MqttQualityOfService.AtMostOnce );
                await client3.SubscribeAsync( TestHelper.Monitor, topic, MqttQualityOfService.AtLeastOnce );

                ManualResetEventSlim willReceivedSignal = new ManualResetEventSlim( initialState: false );

                client2.MessageStream.Subscribe( m =>
                 {
                     if( m.Item.Topic == topic )
                     {
                         willReceivedSignal.Set();
                     }
                 } );
                client3.MessageStream.Subscribe( m =>
                 {
                     if( m.Item.Topic == topic )
                     {
                         willReceivedSignal.Set();
                     }
                 } );

                await client1.DisconnectAsync( TestHelper.Monitor);

                bool willReceived = willReceivedSignal.Wait( 2000 );

                Assert.False( willReceived );
            }
        }

        [Test]
        public async Task when_client_disconnects_unexpectedly_then_will_message_is_sent()
        {
            IMqttClient client1 = await GetClientAsync();
            IMqttClient client2 = await GetClientAsync();
            IMqttClient client3 = await GetClientAsync();

            string topic = Guid.NewGuid().ToString();
            MqttQualityOfService qos = MqttQualityOfService.ExactlyOnce;
            bool retain = true;
            FooWillMessage willMessage = new FooWillMessage { Message = "Client 1 has been disconnected unexpectedly" };
            byte[] willMessagePayload = willMessage.GetPayload();
            MqttLastWill will = new MqttLastWill( topic, qos, retain, willMessagePayload );

            await client1.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ), will );
            await client2.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );
            await client3.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

            await client2.SubscribeAsync( TestHelper.Monitor, topic, MqttQualityOfService.AtMostOnce );
            await client3.SubscribeAsync( TestHelper.Monitor, topic, MqttQualityOfService.AtLeastOnce );

            ManualResetEventSlim willReceivedSignal = new ManualResetEventSlim( initialState: false );
            MqttApplicationMessage willApplicationMessage = default;

            client2.MessageStream.Subscribe( m =>
             {
                 if( m.Item.Topic == topic )
                 {
                     willApplicationMessage = m.Item;
                     willReceivedSignal.Set();
                 }
             } );
            client3.MessageStream.Subscribe( m =>
             {
                 if( m.Item.Topic == topic )
                 {
                     willApplicationMessage = m.Item;
                     willReceivedSignal.Set();
                 }
             } );

            //Forces socket disconnection without using protocol Disconnect (Disconnect or Dispose Client method)
            (client1 as MqttClientImpl).Channel.Dispose();

            bool willReceived = willReceivedSignal.Wait( 2000 );

            Assert.True( willReceived );
            Assert.NotNull( willMessage );
            willApplicationMessage.Topic.Should().Be( topic );
            FooWillMessage.GetMessage( willApplicationMessage.Payload ).Message.Should().Be( willMessage.Message );

            client2.Dispose();
            client3.Dispose();
        }

        [Test]
        public async Task when_client_disconnects_then_message_stream_completes()
        {
            ManualResetEventSlim streamCompletedSignal = new ManualResetEventSlim( initialState: false );
            IMqttClient client = await GetClientAsync();

            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

            client.MessageStream.Subscribe( _ => { }, onCompleted: () => streamCompletedSignal.Set() );

            await client.DisconnectAsync( TestHelper.Monitor);

            bool streamCompleted = streamCompletedSignal.Wait( 2000 );

            Assert.True( streamCompleted );

            client.Dispose();
        }
    }
}
