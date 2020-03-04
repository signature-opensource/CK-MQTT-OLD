using CK.Core;

using System;
using System.Collections;
using System.Collections.Generic;
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
        readonly ReplaySubject<Monitored<MqttApplicationMessage>> _receiver;
        MqttStubClient( IActivityMonitor m, MqttConfiguration config, NamedPipeClientStream namedPipeClientStream )
        {
            _m = m;
            _config = config;
            _pipe = namedPipeClientStream;
            _pipeFormatter = new PipeFormatter( _pipe );
            _listenerCancel = new CancellationTokenSource();
            _listener = Listen( _listenerCancel.Token );
            _receiver = new ReplaySubject<Monitored<MqttApplicationMessage>>();
        }

        public static MqttStubClient Create( IActivityMonitor m, MqttConfiguration config, string pipeName = "mqtt_pipe", string serverName = "." )
        {
            NamedPipeClientStream pipe = new NamedPipeClientStream( serverName, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous );
            return new MqttStubClient( m, config, pipe );
        }

        public async Task Listen( CancellationToken token )
        {
            var m = new ActivityMonitor();
            while( !token.IsCancellationRequested )
            {
                Queue<object> payload = await _pipeFormatter.ReceivePayloadAsync( token );

                if( !(payload.Dequeue() is RelayHeader serverHeader) ) throw new InvalidDataException( "Header was not a RelayHeader." );
                switch( serverHeader )
                {
                    case RelayHeader.Disconnected:
                        Disconnected?.Invoke( this, (MqttEndpointDisconnected)payload.Dequeue() );
                        break;
                    case RelayHeader.MessageEvent:
                        _receiver.OnNext( new Monitored<MqttApplicationMessage>( m, (MqttApplicationMessage)payload.Dequeue() ) );
                        break;
                    default:
                        throw new InvalidDataException( "Unknown Relay Header." );
                }
            }
        }

        public string Id => null;

        public bool IsConnected(IActivityMonitor m) => _pipe.IsConnected;

        public IObservable<Monitored<MqttApplicationMessage>> MessageStream => _receiver;

        public event EventHandler<MqttEndpointDisconnected> Disconnected;

        public async Task<SessionState> ConnectAsync( IActivityMonitor m, MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false )
        {
            await _pipe.ConnectAsync( _config.WaitTimeoutSecs * 1000 );
            if( _pipe.TransmissionMode != PipeTransmissionMode.Message )
            {
                throw new NotSupportedException( "The transmission is only supported in message mode." );
            }
            await _pipeFormatter.SendPayloadAsync(m, StubClientHeader.Connect, credentials, will, cleanSession );
            Stack result = _pipeFormatter.ReceivePayload();
            if( !(result.Pop() is SessionState state) ) throw new InvalidOperationException( "Unexpected payload return type." );
            return state;
        }

        public async Task<SessionState> ConnectAsync( IActivityMonitor m, MqttLastWill will = null )
        {
            await _pipe.ConnectAsync( _config.WaitTimeoutSecs * 1000 );
            if( _pipe.TransmissionMode != PipeTransmissionMode.Message )
            {
                throw new NotSupportedException( "The transmission is only supported in message mode." );
            }
            await _pipeFormatter.SendPayloadAsync(m, StubClientHeader.Connect, will );
            Stack result = _pipeFormatter.ReceivePayload();
            if( !(result.Pop() is SessionState state) ) throw new InvalidOperationException( "Unexpected payload return type." );
            if( result.Count != 0 )
            {
                _m.Warn( "Connect payload returned more than one object." );
            }
            return state;
        }

        public Task DisconnectAsync( IActivityMonitor m ) => _pipeFormatter.SendPayloadAsync(m, StubClientHeader.Disconnect );

        public void Dispose()
        {
            _listenerCancel.Cancel();
            _listener.Wait();
            _pipe.Dispose();
        }

        public Task PublishAsync( IActivityMonitor m, MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false )
            => _pipeFormatter.SendPayloadAsync( m, StubClientHeader.Publish, message, qos, retain );

        public Task SubscribeAsync( IActivityMonitor m, string topicFilter, MqttQualityOfService qos )
            => _pipeFormatter.SendPayloadAsync( m, StubClientHeader.Subscribe, topicFilter, qos );

        public Task UnsubscribeAsync( IActivityMonitor m, params string[] topics )
            => _pipeFormatter.SendPayloadAsync( m, StubClientHeader.Unsubscribe, topics );
    }
}
