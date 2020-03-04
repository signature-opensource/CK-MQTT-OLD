using CK.Core;

using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttRelay : IHostedService
    {
        readonly PipeFormatter _pf;
        private readonly IActivityMonitor _m;
        readonly MqttClientCredentials _credentials;
        readonly MqttLastWill _lastWill;
        readonly bool _cleanSession;
        readonly IMqttClient _client;
        Task _task;
        readonly IDisposable _observing;
        readonly CancellationTokenSource _tokenSource;
        public MqttRelay( IActivityMonitor m, NamedPipeServerStream pipe, MqttClientCredentials credentials, MqttLastWill lastWill, bool cleanSession, IMqttClient client )
        {
            _m = m;
            _credentials = credentials;
            _lastWill = lastWill;
            _cleanSession = cleanSession;
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _pf = new PipeFormatter( pipe );
            _observing = _client.MessageStream.Subscribe( MessageReceived );
            client.Disconnected += Client_Disconnected;
        }

        private void Client_Disconnected( object sender, MqttEndpointDisconnected e )
        {
            _pf.SendPayload( RelayHeader.Disconnected, e );
        }

        void MessageReceived( Monitored<MqttApplicationMessage> msg )
        {
            _pf.SendPayload( RelayHeader.MessageEvent, msg.Item );
        }

        async Task Run( IActivityMonitor m, CancellationToken cancellationToken )
        {
            while( !cancellationToken.IsCancellationRequested )
            {
                Queue<object> payload = await _pf.ReceivePayloadAsync( cancellationToken );
                if( !(payload.Dequeue() is StubClientHeader clientHeader) ) throw new InvalidOperationException( "Payload did not start with a ClientHeader." );
                switch( clientHeader )
                {
                    case StubClientHeader.Disconnect:
                        await _client.DisconnectAsync( m );
                        _m.Warn( $"Disconnect should have empty payload, but the payload contain ${payload.Count} objects." );
                        break;
                    case StubClientHeader.Connect:
                        SessionState state;
                        if( payload.Count == 1 )
                        {
                            state = await _client.ConnectAsync( m, (MqttLastWill)payload.Dequeue() );
                        }
                        else if( payload.Count == 3 )
                        {
                            state = await _client.ConnectAsync( m, (MqttClientCredentials)payload.Dequeue(), (MqttLastWill)payload.Dequeue(), (bool)payload.Dequeue() );
                        }
                        else
                        {
                            throw new InvalidOperationException( "Payload count is incorrect." );
                        }
                        await _pf.SendPayloadAsync( m, state );
                        break;
                    case StubClientHeader.Publish:
                        await _client.PublishAsync( m, (MqttApplicationMessage)payload.Dequeue(), (MqttQualityOfService)payload.Dequeue(), (bool)payload.Dequeue() );
                        break;
                    case StubClientHeader.Subscribe:
                        await _client.SubscribeAsync( m, (string)payload.Dequeue(), (MqttQualityOfService)payload.Dequeue() );
                        break;
                    case StubClientHeader.Unsubscribe:
                        await _client.UnsubscribeAsync( m, (string[])payload.Dequeue() );
                        break;
                    case StubClientHeader.IsConnected:
                        await _pf.SendPayloadAsync( m, _client.IsConnected( m ) );
                        break;
                    default:
                        throw new InvalidOperationException( "Unknown ClientHeader." );
                }
            }
        }

        public async Task StartAsync( CancellationToken cancellationToken )
        {
            await _client.ConnectAsync( _m, _credentials, _lastWill, _cleanSession );
            _task = Run( _m, _tokenSource.Token );
        }

        public async Task StopAsync( CancellationToken cancellationToken )
        {
            _tokenSource.Cancel();
            await _task;
            _observing.Dispose();
            _client.Disconnected -= Client_Disconnected;
        }
    }
}
