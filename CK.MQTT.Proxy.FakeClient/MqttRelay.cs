using CK.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttRelay : IHostedService, IDisposable
    {
        private readonly IActivityMonitor _m;
        readonly MqttClientCredentials _credentials;
        readonly MqttLastWill _lastWill;
        readonly bool _cleanSession;
        readonly IMqttClient _client;
        readonly NamedPipeServerStream _pipe;
        Task _task;
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
            _pipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous );
            _observing = _client.MessageStream.Subscribe( MessageReceived );
            client.Disconnected += Client_Disconnected;
            _anonymous = true;
        }

        void Client_Disconnected( object sender, MqttEndpointDisconnected e )
        {
            using( var msg = new MessageFormatter() )
            {
                msg.Bw.WriteEnum( RelayHeader.Disconnected );
                msg.Bw.Write( e );
                msg.SendMessageAsync( _pipe ).Wait();
            }
        }

        void MessageReceived( MqttApplicationMessage msg )
        {
            using( var output = new MessageFormatter() )
            {
                output.Bw.WriteEnum( RelayHeader.MessageEvent );
                output.Bw.Write( msg );
                output.SendMessageAsync( _pipe ).Wait();
            }
        }

        async Task Run( CancellationToken cancellationToken )
        {
            while( !cancellationToken.IsCancellationRequested )
            {
                if( !_pipe.IsConnected ) await _pipe.WaitForConnectionAsync( cancellationToken );
                if( cancellationToken.IsCancellationRequested ) return;
                using( MemoryStream streamOut = await _pipe.ReadMessageAsync( cancellationToken ) )
                using( CKBinaryReader br = new CKBinaryReader( streamOut ) )
                {
                    if( cancellationToken.IsCancellationRequested ) return;
                    var header = br.ReadEnum<StubClientHeader>();
                    switch( header )
                    {
                        case StubClientHeader.Disconnect:
                            await _client.DisconnectAsync();
                            break;
                        case StubClientHeader.ConnectAnonymous:
                            using( var msg = new MessageFormatter() )
                            {
                                //br.ReadLastWill()
                                //await _client.ConnectAsync( )//TODO
                                msg.Bw.WriteEnum( SessionState.CleanSession );
                                await msg.SendMessageAsync( _pipe );
                            }
                            break;
                        case StubClientHeader.Connect:
                            using( var msg = new MessageFormatter() )
                            {
                                br.ReadClientCredentials();
                                br.ReadLastWill();
                                bool clean = br.ReadBoolean();
                                //await _client.ConnectAsync( br.ReadClientCredentials(), br.ReadLastWill(), br.ReadBoolean() )
                                msg.Bw.WriteEnum( clean ?  SessionState.CleanSession : SessionState.SessionPresent);//TODO
                                await msg.SendMessageAsync( _pipe );
                            }
                            break;
                        case StubClientHeader.Publish:
                            await _client.PublishAsync( br.ReadApplicationMessage(), br.ReadEnum<MqttQualityOfService>(), br.ReadBoolean() );
                            break;
                        case StubClientHeader.Subscribe:

                            await _client.SubscribeAsync( br.ReadString(), br.ReadEnum<MqttQualityOfService>() );
                            break;
                        case StubClientHeader.Unsubscribe:
                            string[] topics = new string[br.Read()];
                            for( int i = 0; i < topics.Length; i++ )
                            {
                                topics[i] = br.ReadString();
                            }
                            await _client.UnsubscribeAsync( topics );
                            break;
                        case StubClientHeader.IsConnected:
                            using( var msg = new MessageFormatter() )
                            {
                                msg.Bw.Write( _client.IsConnected );
                                await msg.SendMessageAsync( _pipe );
                            }
                            break;
                        default:
                            throw new InvalidOperationException( $"Unknown ClientHeader: {header.ToString()}." );
                    }
                }

            }
        }

        public async Task StartAsync( CancellationToken cancellationToken )
        {
            _tokenSource = new CancellationTokenSource();
            if( _anonymous )
            {
                await _client.ConnectAsync( _lastWill );
            }
            else
            {
                await _client.ConnectAsync( _credentials, _lastWill, _cleanSession );
            }
            _task = Run( _tokenSource.Token );
        }

        public async Task StopAsync( CancellationToken cancellationToken )
        {
            _tokenSource.Cancel();
            await _task;
        }

        public void Dispose()
        {
            _client.Disconnected -= Client_Disconnected;
            _observing.Dispose();
            _pipe.Dispose();
        }
    }
}
