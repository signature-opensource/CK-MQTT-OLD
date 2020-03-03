using CK.MQTT;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{
    public abstract class ConnectionSpecWithKeepAlive : IntegrationContext
    {
        protected ConnectionSpecWithKeepAlive()
            : base( keepAliveSecs: 1 )
        {
        }

        [Test]
        public async Task when_keep_alive_enabled_and_client_is_disposed_then_server_refresh_active_client_list()
        {
            IMqttClient client = await GetClientAsync();

            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ) );

            string clientId = client.Id;
            bool existClientAfterConnect = Server.ActiveClients.Any( c => c == clientId );
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
                ex => { Console.WriteLine( $"Error: {ex.Message}" ); } );

            client.Dispose();

            bool serverDetectedClientClosed = clientClosed.Wait( TimeSpan.FromSeconds( KeepAliveSecs * 2 ) );

            subscription.Dispose();

            Assert.True( existClientAfterConnect );
            Assert.True( serverDetectedClientClosed );
            Assert.False( Server.ActiveClients.Any( c => c == clientId ) );
        }

        [Test]
        public async Task when_keep_alive_enabled_and_no_packets_are_sent_then_connection_is_maintained()
        {
            IMqttClient client = await GetClientAsync();
            string clientId = MqttTestHelper.GetClientId();

            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( clientId ) );

            await Task.Delay( TimeSpan.FromSeconds( KeepAliveSecs * 5 ) );

            Assert.True( Server.ActiveClients.Any( c => c == clientId ) );
            Assert.True( client.IsConnected );
            Assert.False( string.IsNullOrEmpty( client.Id ) );

            client.Dispose();
        }
    }
}
