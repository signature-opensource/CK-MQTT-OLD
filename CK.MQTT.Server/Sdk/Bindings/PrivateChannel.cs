using CK.Core;
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
        bool _disposed;
        readonly IActivityMonitor _m;
        readonly PrivateStream _stream;
        readonly EndpointIdentifier _identifier;
        readonly ReplaySubject<(IActivityMonitor, byte[])> _receiver;
        readonly ReplaySubject<(IActivityMonitor, byte[])> _sender;
        readonly IDisposable _streamSubscription;

        public PrivateChannel( IActivityMonitor m, PrivateStream stream, EndpointIdentifier identifier, MqttConfiguration configuration )
        {
            _m = m;
            _stream = stream;
            _identifier = identifier;
            _receiver = new ReplaySubject<(IActivityMonitor, byte[])>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<(IActivityMonitor, byte[])>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _streamSubscription = SubscribeStream();
        }

        public bool IsConnected => !_stream.IsDisposed;

        public IObservable<(IActivityMonitor, byte[])> ReceiverStream => _receiver;

        public IObservable<(IActivityMonitor, byte[])> SenderStream => _sender;

        public Task SendAsync( IActivityMonitor m, byte[] message )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( nameof( PrivateChannel ) );
            }

            if( !IsConnected )
            {
                throw new MqttException( Properties.MqttChannel_ClientNotConnected );
            }

            _sender.OnNext( (m, message) );

            try
            {
                m.Trace( string.Format( Properties.MqttChannel_SendingPacket, message.Length ) );

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
            Dispose( disposing: true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if( _disposed ) return;

            if( disposing )
            {
                _m.Info( string.Format( ServerProperties.Mqtt_Disposing, nameof( PrivateChannel ) ) );

                _streamSubscription.Dispose();
                _receiver.OnCompleted();
                _stream.Dispose();

                _disposed = true;
            }
        }

        IDisposable SubscribeStream()
        {
            var senderIdentifier = _identifier == EndpointIdentifier.Client ?
                EndpointIdentifier.Server :
                EndpointIdentifier.Client;

            return _stream
                .Receive( senderIdentifier )
                .ObserveOn( NewThreadScheduler.Default )
                .Subscribe( packet =>
                {
                    tracer.Verbose( Properties.MqttChannel_ReceivedPacket, packet.Length );

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
                    tracer.Warn( Properties.MqttChannel_NetworkStreamCompleted );
                    _receiver.OnCompleted();
                } );
        }
    }
}
