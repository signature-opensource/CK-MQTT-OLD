using IntegrationTests.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using CK.MQTT;
using CK.MQTT.Sdk;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Tests;

namespace IntegrationTests
{
    public sealed class ConnectionSpec : IntegrationContext, IDisposable
    {
        readonly IMqttServer server;

        public ConnectionSpec()
        {
            server = GetServerAsync().Result;
        }

        [Test]
        public async Task when_connecting_client_to_non_existing_server_then_fails()
        {
            try
            {
                server.Dispose();

                var client = await GetClientAsync();

                await client.ConnectAsync( new MqttClientCredentials( GetClientId() ) );
            }
            catch( Exception ex )
            {
                Assert.True( ex is MqttClientException );
                Assert.NotNull( ex.InnerException );
                Assert.True( ex.InnerException is MqttException );
                Assert.NotNull( ex.InnerException.InnerException );
                Assert.True( ex.InnerException.InnerException is SocketException );
            }
        }

        [Test]
        public async Task when_clients_connect_and_disconnect_then_server_raises_events()
        {
            using( var fooClient = await GetClientAsync() )
            using( var barClient = await GetClientAsync() )
            {

                var clientId1 = GetClientId();
                var clientId2 = GetClientId();

                var connected = new List<string>();
                var disconnected = new List<string>();

                server.ClientConnected += ( sender, id ) => connected.Add( id );
                server.ClientDisconnected += ( sender, id ) =>
                {
                    connected.Remove( id );
                    disconnected.Add( id );
                };

                await fooClient.ConnectAsync( new MqttClientCredentials( clientId1 ) );

                connected.Should().BeEquivalentTo( clientId1 );

                await barClient.ConnectAsync( new MqttClientCredentials( clientId2 ) );

                connected.Should().BeEquivalentTo( clientId1, clientId2 );

                await barClient.DisconnectAsync();

                disconnected.Should().BeEquivalentTo( clientId2 );
                connected.Should().BeEquivalentTo( clientId1 );

                await fooClient.DisconnectAsync();

                disconnected.Should().BeEquivalentTo( clientId2, clientId1 );
                connected.Should().BeEmpty();
            }
        }

        [Test]
        public async Task when_connect_clients_and_one_client_drops_connection_then_other_client_survives()
        {
            using( var fooClient = await GetClientAsync() )
            using( var barClient = await GetClientAsync() )
            {
                var fooClientId = GetClientId();
                var barClientId = GetClientId();

                await fooClient.ConnectAsync( new MqttClientCredentials( fooClientId ) );
                await barClient.ConnectAsync( new MqttClientCredentials( barClientId ) );

                var clientIds = new List<string> { fooClientId, barClientId };
                var initialConnectedClients = server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();
                var exceptionThrown = false;

                try
                {
                    //Force an exception to be thrown by publishing null message
                    await fooClient.PublishAsync( message: null, qos: MqttQualityOfService.AtMostOnce );
                }
                catch
                {
                    exceptionThrown = true;
                }

                var serverSignal = new ManualResetEventSlim();

                while( !serverSignal.IsSet )
                {
                    if( !server.ActiveClients.Any( c => c == fooClientId ) )
                    {
                        serverSignal.Set();
                    }
                }

                serverSignal.Wait();

                var finalConnectedClients = server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();

                initialConnectedClients.Should().Be( 2 );
                Assert.True( exceptionThrown );
                finalConnectedClients.Should().Be( 1 );
            }
        }

        [Test]
        public async Task when_connect_clients_then_succeeds()
        {
            var count = GetTestLoad();
            var clients = new List<IMqttClient>();
            var clientIds = new List<string>();
            var tasks = new List<Task>();

            for( var i = 1; i <= count; i++ )
            {
                var client = await GetClientAsync();
                var clientId = GetClientId();

                tasks.Add( client.ConnectAsync( new MqttClientCredentials( clientId ) ) );
                clients.Add( client );
                clientIds.Add( clientId );
            }

            await Task.WhenAll( tasks );

            server.ActiveClients.Where( c => clientIds.Contains( c ) ).Should().HaveCount( count );
            Assert.True( clients.All( c => c.IsConnected ) );
            Assert.True( clients.All( c => !string.IsNullOrEmpty( c.Id ) ) );

            foreach( var client in clients )
            {
                client.Dispose();
            }
        }

        [Test]
        public async Task when_connecting_twice_with_same_client_with_disconnecting_then_succeeds()
        {
            using( var client = await GetClientAsync() )
            {
                var clientId = GetClientId();

                await client.ConnectAsync( new MqttClientCredentials( clientId ) );
                await client.DisconnectAsync();
                await client.ConnectAsync( new MqttClientCredentials( clientId ) );
                server.ActiveClients.Count( c => c == clientId ).Should().Be( 1 );
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
            var clientId = GetClientId();
            using( var clientFoo = await GetClientAsync() )
            using( var clientBar = await GetClientAsync() )
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

                server.ClientDisconnected += onDisconnect;
                server.ClientConnected += onConnect;

                await clientFoo.ConnectAsync( new MqttClientCredentials( clientId ) );
                bool didNotTimeout = true;
                lock( events )
                {
                    if( events.Count < 1 ) didNotTimeout = Monitor.Wait( events, 500 );
                }

                Assert.True( didNotTimeout );
                string.Concat( events ).Should().Be( "c" );

                await clientBar.ConnectAsync( new MqttClientCredentials( clientId ) );
                lock( events )
                {
                    while( events.Count < 4 ) didNotTimeout &= Monitor.Wait( events, 500 );
                }

                Assert.True( didNotTimeout );
                string.Concat( events ).Should().Be( "ccdd" );
                server.ClientDisconnected -= onDisconnect;
                server.ClientConnected -= onConnect;
                //"cdc".Should().Be(events ); //Require MQTT-3.1.4-2
            }
        }

        [Test]
        public async Task when_connecting_twice_with_same_client_without_disconnecting_then_fails()
        {
            var client = await GetClientAsync();
            var clientId = GetClientId();

            await client.ConnectAsync( new MqttClientCredentials( clientId ) );

            var ex = Assert.Throws<AggregateException>( () => client.ConnectAsync( new MqttClientCredentials( clientId ) ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
        }

        [Test]
        public async Task when_connecting_client_with_invalid_id_then_fails()
        {
            var client = await GetClientAsync();
            var clientId = "#invalid*client-id";

            var ex = Assert.Throws<AggregateException>( () => client.ConnectAsync( new MqttClientCredentials( clientId ) ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
            Assert.NotNull( ex.InnerException.InnerException );
            Assert.True( ex.InnerException.InnerException is MqttException );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_then_succeeds()
        {
            var client = await GetClientAsync();

            await client.ConnectAsync();

            Assert.True( client.IsConnected );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
            client.Id.Should().StartWith( "anonymous" );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_and_clean_session_true_then_succeeds()
        {
            var client = await GetClientAsync();

            await client.ConnectAsync( new MqttClientCredentials( clientId: null ), cleanSession: true );

            Assert.True( client.IsConnected );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
            client.Id.Should().StartWith( "anonymous" );
        }

        [Test]
        public async Task when_connecting_client_with_empty_id_and_clean_session_false_then_fails()
        {
            var client = await GetClientAsync();
            var clientId = string.Empty;

            var ex = Assert.Throws<AggregateException>( () => client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: false ).Wait() );

            Assert.NotNull( ex );
            Assert.NotNull( ex.InnerException );
            Assert.True( ex.InnerException is MqttClientException );
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_true_then_session_is_not_preserved()
        {
            using (var client = await GetClientAsync())
            {
                var clientId = GetClientId();

                var sessionState1 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync();

                var sessionState2 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync();
                sessionState1.Should().Be( SessionState.CleanSession );
                sessionState2.Should().Be( SessionState.CleanSession );

                client.Dispose();
            }
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_false_and_then_true_then_session_is_not_preserved()
        {
            using (var client = await GetClientAsync())
            {
                var clientId = GetClientId();

                var sessionState1 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: false );

                await client.DisconnectAsync();

                var sessionState2 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: true );

                await client.DisconnectAsync();
                sessionState1.Should().Be( SessionState.CleanSession );
                sessionState2.Should().Be( SessionState.CleanSession );
            }
        }

        [Test]
        public async Task when_connecting_client_with_clean_session_false_then_session_is_preserved()
        {
            var client = await GetClientAsync();
            var clientId = GetClientId();

            var sessionState1 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: false );

            await client.DisconnectAsync();

            var sessionState2 = await client.ConnectAsync( new MqttClientCredentials( clientId ), cleanSession: false );

            await client.DisconnectAsync();

            sessionState1.Should().Be( SessionState.CleanSession );
            sessionState2.Should().Be( SessionState.SessionPresent );

            client?.Dispose();
        }

        [Test]
        public async Task when_disconnect_clients_then_succeeds()
        {
            var count = GetTestLoad();
            var clients = new List<IMqttClient>();
            var clientIds = new List<string>();

            for( var i = 1; i <= count; i++ )
            {
                var client = await GetClientAsync();
                var clientId = GetClientId();

                await client.ConnectAsync( new MqttClientCredentials( clientId ) );
                clients.Add( client );
                clientIds.Add( clientId );
            }

            var initialConnectedClients = server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count();

            foreach( var client in clients )
            {
                await client.DisconnectAsync();
            }

            var disconnectedSignal = new ManualResetEventSlim( initialState: false );

            while( !disconnectedSignal.IsSet )
            {
                if( server.ActiveClients.Where( c => clientIds.Contains( c ) ).Count() == 0 && clients.All( c => !c.IsConnected ) )
                {
                    disconnectedSignal.Set();
                }
            }

            initialConnectedClients.Should().Be( clients.Count );
            server.ActiveClients.Where( c => clientIds.Contains( c ) ).Should().BeEmpty();
            Assert.True( clients.All( c => !c.IsConnected ) );
            Assert.True( clients.All( c => string.IsNullOrEmpty( c.Id ) ) );

            foreach( var client in clients )
            {
                client.Dispose();
            }
        }

        [Test]
        public async Task when_disconnect_client_then_server_decrease_active_client_list()
        {
            using( var client = await GetClientAsync() )
            {

                await client.ConnectAsync( new MqttClientCredentials( GetClientId() ) )
                    .ConfigureAwait( continueOnCapturedContext: false );

                var clientId = client.Id;
                var existClientAfterConnect = server.ActiveClients.Any( c => c == clientId );

                await client.DisconnectAsync()
                    .ConfigureAwait( continueOnCapturedContext: false );

                var clientClosed = new ManualResetEventSlim();

                var subscription = Observable.Create<bool>( observer =>
                 {
                     var timer = new System.Timers.Timer();

                     timer.Interval = 200;
                     timer.Elapsed += ( sender, args ) =>
                     {
                         if( server.ActiveClients.Any( c => c == clientId ) )
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

                var clientDisconnected = clientClosed.Wait( 2000 );

                Assert.True( existClientAfterConnect );
                Assert.True( clientDisconnected );
                server.ActiveClients.Should().NotContain( c => c == clientId );
            }
        }

        [Test]
        public async Task when_client_disconnects_by_protocol_then_will_message_is_not_sent()
        {
            using( var client1 = await GetClientAsync() )
            using( var client2 = await GetClientAsync() )
            using( var client3 = await GetClientAsync() )
            {

                var topic = Guid.NewGuid().ToString();
                var qos = MqttQualityOfService.ExactlyOnce;
                var retain = true;
                var willMessage = new FooWillMessage { Message = "Client 1 has been disconnected unexpectedly" };
                var will = new MqttLastWill( topic, qos, retain, willMessage.GetPayload() );

                await client1.ConnectAsync( new MqttClientCredentials( GetClientId() ), will );
                await client2.ConnectAsync( new MqttClientCredentials( GetClientId() ) );
                await client3.ConnectAsync( new MqttClientCredentials( GetClientId() ) );

                await client2.SubscribeAsync( topic, MqttQualityOfService.AtMostOnce );
                await client3.SubscribeAsync( topic, MqttQualityOfService.AtLeastOnce );

                var willReceivedSignal = new ManualResetEventSlim( initialState: false );

                client2.MessageStream.Subscribe( m =>
                 {
                     if( m.Topic == topic )
                     {
                         willReceivedSignal.Set();
                     }
                 } );
                client3.MessageStream.Subscribe( m =>
                 {
                     if( m.Topic == topic )
                     {
                         willReceivedSignal.Set();
                     }
                 } );

                await client1.DisconnectAsync();

                var willReceived = willReceivedSignal.Wait( 2000 );

                Assert.False( willReceived );
            }
        }

        [Test]
        public async Task when_client_disconnects_unexpectedly_then_will_message_is_sent()
        {
            var client1 = await GetClientAsync();
            var client2 = await GetClientAsync();
            var client3 = await GetClientAsync();

            var topic = Guid.NewGuid().ToString();
            var qos = MqttQualityOfService.ExactlyOnce;
            var retain = true;
            var willMessage = new FooWillMessage { Message = "Client 1 has been disconnected unexpectedly" };
            var willMessagePayload = willMessage.GetPayload();
            var will = new MqttLastWill( topic, qos, retain, willMessagePayload );

            await client1.ConnectAsync( new MqttClientCredentials( GetClientId() ), will );
            await client2.ConnectAsync( new MqttClientCredentials( GetClientId() ) );
            await client3.ConnectAsync( new MqttClientCredentials( GetClientId() ) );

            await client2.SubscribeAsync( topic, MqttQualityOfService.AtMostOnce );
            await client3.SubscribeAsync( topic, MqttQualityOfService.AtLeastOnce );

            var willReceivedSignal = new ManualResetEventSlim( initialState: false );
            var willApplicationMessage = default( MqttApplicationMessage );

            client2.MessageStream.Subscribe( (Action<MqttApplicationMessage>)(m =>
             {
                 if( m.Topic == topic )
                 {
                     willApplicationMessage = m;
                     willReceivedSignal.Set();
                 }
             }) );
            client3.MessageStream.Subscribe( (Action<MqttApplicationMessage>)(m =>
             {
                 if( m.Topic == topic )
                 {
                     willApplicationMessage = m;
                     willReceivedSignal.Set();
                 }
             }) );

            //Forces socket disconnection without using protocol Disconnect (Disconnect or Dispose Client method)
            (client1 as MqttClientImpl).Channel.Dispose();

            var willReceived = willReceivedSignal.Wait( 2000 );

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
            var streamCompletedSignal = new ManualResetEventSlim( initialState: false );
            var client = await GetClientAsync();

            await client.ConnectAsync( new MqttClientCredentials( GetClientId() ) );

            client.MessageStream.Subscribe( _ => { }, onCompleted: () => streamCompletedSignal.Set() );

            await client.DisconnectAsync();

            var streamCompleted = streamCompletedSignal.Wait( 2000 );

            Assert.True( streamCompleted );

            client.Dispose();
        }

        public void Dispose() => server?.Dispose();
    }
}
