using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannel : IMqttChannel<byte[]>
    {
        bool _disposed;
        readonly IActivityMonitor _m;
        readonly TcpClient _client;
        readonly IPacketBuffer _buffer;
        readonly ReplaySubject<Monitored<byte[]>> _receiver;
        readonly ReplaySubject<Monitored<byte[]>> _sender;
        readonly IDisposable _streamSubscription;

        public TcpChannel(
            IActivityMonitor m,
            TcpClient client,
            IPacketBuffer buffer,
            MqttConfiguration configuration )
        {
            _m = m;
            _client = client;
            _buffer = buffer;
            _receiver = new ReplaySubject<Monitored<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ));
            _sender = new ReplaySubject<Monitored<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ));
            _streamSubscription = SubscribeStream();
        }

        public bool IsConnected
        {
            get
            {
                var connected = !_disposed;

                try
                {
                    connected = connected && _client.Connected;
                }
                catch( Exception )
                {
                    connected = false;
                }

                return connected;
            }
        }

        public IObservable<Monitored<byte[]>> ReceiverStream => _receiver;

        public IObservable<Monitored<byte[]>> SenderStream => _sender;

        public async Task SendAsync( IActivityMonitor m, byte[] message )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            if( !IsConnected ) throw new MqttException( Properties.MqttChannel_ClientNotConnected );

            _sender.OnNext( (m, message) );

            try
            {
                m.Trace( string.Format( Properties.MqttChannel_SendingPacket, message.Length ) );

                await _client.GetStream()
                    .WriteAsync( message, 0, message.Length )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( Properties.MqttChannel_StreamDisconnected, disposedEx );
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if( _disposed ) return;

            if( disposing )
            {
                _m.Info( string.Format( Properties.Mqtt_Disposing, GetType().FullName ) );

                _streamSubscription.Dispose();
                _receiver.OnCompleted();

                try
                {
                    _client?.Dispose();
                }
                catch( SocketException socketEx )
                {
                    _m.Error( string.Format( Properties.MqttChannel_DisposeError, socketEx.SocketErrorCode ), socketEx );
                }

                _disposed = true;
            }
        }

        IDisposable SubscribeStream()
        {
            var m = new ActivityMonitor();
            return Observable.Defer( () =>
            {
                var buffer = new byte[_client.ReceiveBufferSize];

                return Observable.FromAsync(
                    () => _client.GetStream().ReadAsync( buffer, 0, buffer.Length )
                )
                .Select( x => buffer.Take( x ) );
            } )
            .Repeat()
            .TakeWhile( bytes => bytes.Any() )
            .ObserveOn( NewThreadScheduler.Default )
            .Subscribe( bytes =>
            {

                if( _buffer.TryGetPackets( bytes, out IEnumerable<byte[]> packets ) )
                {
                    foreach( var packet in packets )
                    {
                        m.Trace( string.Format( Properties.MqttChannel_ReceivedPacket, packet.Length ) );
                        _receiver.OnNext( (m, packet) );
                    }
                }
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
                m.Warn( Properties.MqttChannel_NetworkStreamCompleted );
                _receiver.OnCompleted();
            } );
        }
    }
}
