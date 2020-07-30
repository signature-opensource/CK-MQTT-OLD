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
        readonly ReplaySubject<Mon<byte[]>> _receiver;
        readonly ReplaySubject<Mon<byte[]>> _sender;
        readonly IDisposable _streamSubscription;

        public PrivateChannel( IActivityMonitor m, PrivateStream stream, EndpointIdentifier identifier, MqttConfiguration configuration )
        {
            _m = m;
            _stream = stream;
            _identifier = identifier;
            _receiver = new ReplaySubject<Mon<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<Mon<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _streamSubscription = SubscribeStream();
        }

        public bool IsConnected => !_stream.IsDisposed;

        public IObservable<Mon<byte[]>> ReceiverStream => _receiver;

        public IObservable<Mon<byte[]>> SenderStream => _sender;

        public Task SendAsync( Mon<byte[]> message )
        {
            if( _disposed ) throw new ObjectDisposedException( nameof( PrivateChannel ) );

            if( !IsConnected ) throw new MqttException( ClientProperties.MqttChannel_ClientNotConnected );

            _sender.OnNext( message );

            try
            {
                message.Monitor.Trace( ClientProperties.MqttChannel_SendingPacket( message.Item.Length ) );
                _stream.Send( message, _identifier );
                return Task.FromResult( true );
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( ClientProperties.MqttChannel_StreamDisconnected, disposedEx );
            }
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _m.Info( ClientProperties.Mqtt_Disposing( nameof( PrivateChannel ) ) );

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
                .Subscribe( packet =>
                {
                    packet.Monitor.Trace( ClientProperties.MqttChannel_ReceivedPacket( packet.Item.Length ) );

                    _receiver.OnNext( packet );
                }, ex =>
                {
                    if( ex is ObjectDisposedException )
                    {
                        _receiver.OnError( new MqttException( ClientProperties.MqttChannel_StreamDisconnected, ex ) );
                    }
                    else
                    {
                        _receiver.OnError( ex );
                    }
                }, () =>
                {
                    var m = new ActivityMonitor(); //TODO: Remove new in the oncomplete.
                    m.Warn( ClientProperties.MqttChannel_NetworkStreamCompleted );
                    _receiver.OnCompleted();
                } );
        }
    }
}
