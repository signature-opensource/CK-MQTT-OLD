using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class PrivateChannel : IMqttChannel<byte[]>
    {
        static readonly ITracer _tracer = Tracer.Get<PrivateChannel>();

        bool _disposed;

        readonly PrivateStream _stream;
        readonly EndpointIdentifier _identifier;
        readonly ReplaySubject<byte[]> _receiver;
        readonly ReplaySubject<byte[]> _sender;
        readonly IDisposable _streamSubscription;

        public PrivateChannel( PrivateStream stream, EndpointIdentifier identifier, MqttConfiguration configuration )
        {
            _stream = stream;
            _identifier = identifier;
            _receiver = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _streamSubscription = SubscribeStream();
        }

        public bool IsConnected => !_stream.IsDisposed;

        public IObservable<byte[]> ReceiverStream => _receiver;

        public IObservable<byte[]> SenderStream => _sender;

        public Task SendAsync( byte[] message )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateChannel ) );

            if( !IsConnected ) throw new MqttException( Properties.MqttChannel_ClientNotConnected );

            _sender.OnNext( message );

            try
            {
                _tracer.Verbose( Properties.MqttChannel_SendingPacket, message.Length );
                _stream.Send( message, _identifier );
                return Task.FromResult( true );
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( Properties.MqttChannel_StreamDisconnected, disposedEx );
            }
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _tracer.Info( ServerProperties.Mqtt_Disposing, nameof( PrivateChannel ) );

            _streamSubscription.Dispose();
            _receiver.OnCompleted();
            _stream.Dispose();

            _disposed = true;
        }

        IDisposable SubscribeStream()
        {
            EndpointIdentifier senderIdentifier = _identifier == EndpointIdentifier.Client ?
                EndpointIdentifier.Server :
                EndpointIdentifier.Client;

            return _stream
                .Receive( senderIdentifier )
                .ObserveOn( NewThreadScheduler.Default )
                .Subscribe( packet =>
                {
                    _tracer.Verbose( Properties.MqttChannel_ReceivedPacket, packet.Length );

                    _receiver.OnNext( packet );
                }, ex =>
                {
                    if( ex is ObjectDisposedException )
                    {
                        _receiver.OnError( new MqttException( Properties.MqttChannel_StreamDisconnected, ex ) );
                    }
                    else
                    {
                        _receiver.OnError( ex );
                    }
                }, () =>
                {
                    _tracer.Warn( Properties.MqttChannel_NetworkStreamCompleted );
                    _receiver.OnCompleted();
                } );
        }
    }
}
