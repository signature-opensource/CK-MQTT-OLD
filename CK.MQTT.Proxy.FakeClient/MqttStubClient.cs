using CK.Core;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CK.MQTT.Proxy.FakeClient
{
    public class MqttStubClient : IMqttClient
    {
        readonly IActivityMonitor _m;
        readonly MqttConfiguration _config;
        readonly NamedPipeClientStream _pipe;
        readonly PipeFormatter _pipeFormatter;
        readonly CancellationTokenSource _listenerCancel;
        readonly Task _listener;
        readonly ReplaySubject<MqttApplicationMessage> _receiver;
        MqttStubClient( IActivityMonitor m, MqttConfiguration config, NamedPipeClientStream namedPipeClientStream )
        {
            _m = m;
            _config = config;
            _pipe = namedPipeClientStream;
            _pipeFormatter = new PipeFormatter( _pipe );
            _listenerCancel = new CancellationTokenSource();
            _listener = Listen( _listenerCancel.Token );
            _receiver = new ReplaySubject<MqttApplicationMessage>();
        }

        public static MqttStubClient Create( IActivityMonitor m, MqttConfiguration config, string pipeName = "mqtt_pipe", string serverName = "." )
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream( serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous );
            return new MqttStubClient( m, config, pipe );
        }

        public async Task Listen( CancellationToken token )
        {
            while( !token.IsCancellationRequested )
            {
                System.Collections.Generic.Queue<object> payload = await _pipeFormatter.ReceivePayloadAsync( token );

                if( !(payload.Dequeue() is RelayHeader serverHeader) ) throw new InvalidDataException( "Header was not a RelayHeader." );
                switch( serverHeader )
                {
                    case RelayHeader.Disconnected:
                        Disconnected?.Invoke( this, (MqttEndpointDisconnected)payload.Dequeue() );
                        break;
                    case RelayHeader.MessageEvent:
                        _receiver.OnNext( (MqttApplicationMessage)payload.Dequeue() );
                        break;
                    default:
                        throw new InvalidDataException( "Unknown Relay Header." );
                }
            }
        }

        public string Id => null;

        public bool IsConnected => _pipe.IsConnected;

        public IObservable<MqttApplicationMessage> MessageStream => _receiver;

        public event EventHandler<MqttEndpointDisconnected> Disconnected;

        public async Task<SessionState> ConnectAsync( MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            await _pipe.ConnectAsync( _config.WaitTimeoutSecs * 1000 );
            if( _pipe.TransmissionMode != PipeTransmissionMode.Message )
            {
                throw new NotSupportedException( "The transmission is only supported in message mode." );
            }
            await _pipeFormatter.SendPayloadAsync( StubClientHeader.Connect, credentials, will, cleanSession );
            Stack result = _pipeFormatter.ReceivePayload();
            if( !(result.Pop() is SessionState state) ) throw new InvalidOperationException( "Unexpected payload return type." );
            return state;
        }

        public async Task<SessionState> ConnectAsync( MqttLastWill will = null )
        {
            await _pipe.ConnectAsync( _config.WaitTimeoutSecs * 1000 );
            if( _pipe.TransmissionMode != PipeTransmissionMode.Message )
            {
                throw new NotSupportedException( "The transmission is only supported in message mode." );
            }
            await _pipeFormatter.SendPayloadAsync( StubClientHeader.Connect, will );
            Stack result = _pipeFormatter.ReceivePayload();
            if( !(result.Pop() is SessionState state) ) throw new InvalidOperationException( "Unexpected payload return type." );
            if( result.Count != 0 )
            {
                _m.Warn( "Connect payload returned more than one object." );
            }
            return state;
        }

        public Task DisconnectAsync() => _pipeFormatter.SendPayloadAsync( StubClientHeader.Disconnect );

        public void Dispose()
        {
            _listenerCancel.Cancel();
            _listener.Wait();
            _pipe.Dispose();
        }

        public Task PublishAsync( MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
            => _pipeFormatter.SendPayloadAsync( StubClientHeader.Publish, message, qos, retain );

        public Task SubscribeAsync( string topicFilter, MqttQualityOfService qos )
            => _pipeFormatter.SendPayloadAsync( StubClientHeader.Subscribe, topicFilter, qos );

        public Task UnsubscribeAsync( params string[] topics )
            => _pipeFormatter.SendPayloadAsync( StubClientHeader.Unsubscribe, topics );
    }
}
