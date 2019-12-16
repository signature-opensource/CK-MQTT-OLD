using CK.Core;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttRelay : IHostedService
    {
        readonly PipeFormatter _pf;
        private readonly IActivityMonitor _m;
        readonly NamedPipeServerStream _pipe;
        readonly MqttClientCredentials _credentials;
        readonly MqttLastWill _lastWill;
        readonly bool _cleanSession;
        readonly IMqttClient _client;
        Task _task;
        IDisposable _observing;
        readonly CancellationTokenSource _tokenSource;
        public MqttRelay( IActivityMonitor m, NamedPipeServerStream pipe, MqttClientCredentials credentials, MqttLastWill lastWill, bool cleanSession, IMqttClient client )
        {
            _m = m;
            _pipe = pipe;
            _credentials = credentials;
            _lastWill = lastWill;
            _cleanSession = cleanSession;
            _client = client;
            _tokenSource = new CancellationTokenSource();
            _pf = new PipeFormatter( pipe );

            _client.MessageStream.Subscribe(MessageReceived);
        }

        void MessageReceived( MqttApplicationMessage msg )
        {
             _pf.SendPayload( ServerHeader.MessageEvent, msg );
        }

        async Task Run( CancellationToken cancellationToken )
        {
            while( !cancellationToken.IsCancellationRequested )
            {
                Queue<object> payload = await _pf.ReceivePayloadAsync( cancellationToken );
                if( !(payload.Dequeue() is ClientHeader clientHeader) ) throw new InvalidOperationException( "Payload did not start with a ClientHeader." );
                switch( clientHeader )
                {
                    case ClientHeader.Disconnect:
                        await _client.DisconnectAsync();
                        _m.Warn( $"Disconnect should have empty payload, but the payload contain ${payload.Count} objects." );
                        break;
                    case ClientHeader.Connect:
                        SessionState state;
                        if( payload.Count == 1 )
                        {
                            state = await _client.ConnectAsync( (MqttLastWill)payload.Dequeue() );
                        }
                        else if( payload.Count == 3 )
                        {
                            state = await _client.ConnectAsync( (MqttClientCredentials)payload.Dequeue(), (MqttLastWill)payload.Dequeue(), (bool)payload.Dequeue() );
                        }
                        else
                        {
                            throw new InvalidOperationException( "Payload count is incorrect." );
                        }
                        await _pf.SendPayloadAsync( state );
                        break;
                    case ClientHeader.Publish:
                        await _client.PublishAsync( (MqttApplicationMessage)payload.Dequeue(), (MqttQualityOfService)payload.Dequeue(), (bool)payload.Dequeue() );
                        break;
                    case ClientHeader.Subscribe:
                        await _client.SubscribeAsync( (string)payload.Dequeue(), (MqttQualityOfService)payload.Dequeue() );
                        break;
                    case ClientHeader.Unsubscribe:
                        await _client.UnsubscribeAsync( (string[])payload.Dequeue() );
                        break;
                    case ClientHeader.IsConnected:
                        await _pf.SendPayloadAsync( _client.IsConnected );
                        break;
                    default:
                        throw new InvalidOperationException( "Unknown ClientHeader." );
                }
            }
        }

        public async Task StartAsync( CancellationToken cancellationToken )
        {
            await _client.ConnectAsync( _credentials, _lastWill, _cleanSession );
            _task = Run( _tokenSource.Token );
        }

        public async Task StopAsync( CancellationToken cancellationToken )
        {
            _tokenSource.Cancel();
            await _task;
            _observing.Dispose();
        }
    }
}
