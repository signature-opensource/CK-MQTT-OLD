using CK.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttRelay : IHostedService, IDisposable
    {
        readonly IActivityMonitor _m;
        readonly MqttClientCredentials _credentials;
        readonly MqttLastWill _lastWill;
        readonly bool _cleanSession;
        readonly string _pipeName;
        readonly IMqttClient _client;
        Task _acceptClients;
        ConcurrentDictionary<Task, PipeStream> _clientsProcessors;
        readonly IDisposable _observing;
        CancellationTokenSource _tokenSource;
        readonly bool _anonymous;
        public MqttRelay(
            IActivityMonitor m,
            IMqttClient client,
            MqttClientCredentials credentials,
            bool cleanSession,
            string pipeName = "ck_mqtt",
            MqttLastWill lastWill = null ) : this( m, client, pipeName, lastWill )
        {
            _credentials = credentials;
            _cleanSession = cleanSession;
            _anonymous = false;
        }

        public MqttRelay( IActivityMonitor m, IMqttClient client, string pipeName = "ck_mqtt", MqttLastWill lastWill = null )
        {
            _m = m;
            _lastWill = lastWill;
            _client = client;
            _observing = _client.MessageStream.Subscribe( MessageReceived );
            client.Disconnected += Client_Disconnected;
            _anonymous = true;
            _pipeName = pipeName;
        }

        void Client_Disconnected( object sender, MqttEndpointDisconnected e )
        {
            using( var msg = new MessageFormatter( RelayHeader.Disconnected ) )
            {
                msg.Bw.Write( e );
                msg.SendMessageAsync( _clientsProcessors.Values ).Wait();
            }
        }

        void MessageReceived( MqttApplicationMessage msg )
        {
            using( var output = new MessageFormatter( RelayHeader.MessageEvent ) )
            {
                output.Bw.Write( msg );
                output.SendMessageAsync( _clientsProcessors.Values ).Wait();
            }
        }


        async Task ProcessClient( NamedPipeServerStream pipe, CancellationToken cancellationToken )
        {
            while( !cancellationToken.IsCancellationRequested && pipe.IsConnected )
            {
                (StubClientHeader header, CKBinaryReader reader) = await pipe.ReadStubMessage( cancellationToken );
                using( reader )
                {
                    if( cancellationToken.IsCancellationRequested ) return;
                    if( !pipe.IsConnected ) return;
                    switch( header )
                    {
                        case StubClientHeader.EndOfStream:
                            return;
                        case StubClientHeader.Disconnect:
                            //await _client.DisconnectAsync();
                            break;
                        case StubClientHeader.ConnectAnonymous:
                            using( var msg = new MessageFormatter( RelayHeader.ConnectResponse ) )
                            {
                                //br.ReadLastWill()
                                //await _client.ConnectAsync( )//TODO
                                msg.Bw.WriteEnum( SessionState.CleanSession );
                                await msg.SendMessageAsync( pipe );
                            }
                            break;
                        case StubClientHeader.Connect:
                            using( var msg = new MessageFormatter( RelayHeader.ConnectResponse ) )
                            {
                                reader.ReadClientCredentials();
                                reader.ReadLastWill();
                                bool clean = reader.ReadBoolean();
                                //await _client.ConnectAsync( br.ReadClientCredentials(), br.ReadLastWill(), br.ReadBoolean() )
                                msg.Bw.WriteEnum( clean ? SessionState.CleanSession : SessionState.SessionPresent );//TODO
                                await msg.SendMessageAsync( pipe );
                            }
                            break;
                        case StubClientHeader.Publish:
                            await _client.PublishAsync( reader.ReadApplicationMessage(), reader.ReadEnum<MqttQualityOfService>(), reader.ReadBoolean() );
                            break;
                        case StubClientHeader.Subscribe:

                            await _client.SubscribeAsync( reader.ReadString(), reader.ReadEnum<MqttQualityOfService>() );
                            break;
                        case StubClientHeader.Unsubscribe:
                            string[] topics = new string[reader.Read()];
                            for( int i = 0; i < topics.Length; i++ )
                            {
                                topics[i] = reader.ReadString();
                            }
                            await _client.UnsubscribeAsync( topics );
                            break;
                        case StubClientHeader.IsConnected:
                            using( var msg = new MessageFormatter( RelayHeader.IsConnectedResponse ) )
                            {
                                msg.Bw.Write( _client.IsConnected );
                                await msg.SendMessageAsync( pipe );
                            }
                            break;
                        default:
                            throw new InvalidOperationException( $"Unknown ClientHeader: {header.ToString()}." );
                    }
                }

            }
        }

        async Task AcceptClients( CancellationToken cancellationToken )
        {
            while( !cancellationToken.IsCancellationRequested )
            {
                var pipe = new NamedPipeServerStream(
                  _pipeName,
                  PipeDirection.InOut,
                  NamedPipeServerStream.MaxAllowedServerInstances,
                  PipeTransmissionMode.Message,
                  PipeOptions.Asynchronous );
                await pipe.WaitForConnectionAsync( cancellationToken );
                if( cancellationToken.IsCancellationRequested ) return;
                if( !_clientsProcessors.TryAdd( ProcessClient( pipe, cancellationToken ), pipe ) )
                {
                    throw new InvalidOperationException( "Newly created Task already existed in the Dictionary." );
                }
            }

        }

        public async Task StartAsync( CancellationToken cancellationToken )
        {
            _clientsProcessors = new ConcurrentDictionary<Task, PipeStream>();
            _tokenSource = new CancellationTokenSource();
            if( _anonymous )
            {
                await _client.ConnectAsync( _lastWill );
            }
            else
            {
                await _client.ConnectAsync( _credentials, _lastWill, _cleanSession );
            }
            _acceptClients = AcceptClients( _tokenSource.Token );
        }

        public async Task StopAsync( CancellationToken cancellationToken )
        {
            _tokenSource.Cancel();
            try
            {
                Task.WaitAll( _clientsProcessors.Keys.ToArray(), cancellationToken );
            }
            catch( TaskCanceledException e)
            {
                _m.Warn( e );
            }
            foreach( var pipe in _clientsProcessors.Values )
            {
                pipe.Dispose();
            }
            await _acceptClients;
        }

        public void Dispose()
        {
            StopAsync( CancellationToken.None ).Wait( 100 );
            _client.Disconnected -= Client_Disconnected;
            _observing.Dispose();
        }
    }
}
