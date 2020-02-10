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
        static readonly ITracer _tracer = Tracer.Get<TcpChannel>();

        bool _disposed;

        readonly TcpClient _client;
        readonly IPacketBuffer _buffer;
        readonly ReplaySubject<byte[]> _receiver;
        readonly ReplaySubject<byte[]> _sender;
        readonly IDisposable _streamSubscription;

        public TcpChannel( TcpClient client,
            IPacketBuffer buffer,
            MqttConfiguration configuration )
        {
            _client = client;
            _client.ReceiveBufferSize = configuration.BufferSize;
            _client.SendBufferSize = configuration.BufferSize;
            _buffer = buffer;
            _receiver = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _streamSubscription = SubscribeStream();
        }

        public bool IsConnected
        {
            get
            {
                bool connected = !_disposed;

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

        public IObservable<byte[]> ReceiverStream { get { return _receiver; } }

        public IObservable<byte[]> SenderStream { get { return _sender; } }

        public async Task SendAsync( byte[] message )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            if( !IsConnected )
            {
                throw new MqttException( Properties.Resources.GetString( "MqttChannel_ClientNotConnected" ) );
            }

            _sender.OnNext( message );

            try
            {
                _tracer.Verbose( Properties.Resources.GetString( "MqttChannel_SendingPacket" ), message.Length );

                await _client.GetStream()
                    .WriteAsync( message, 0, message.Length )
                    .ConfigureAwait( continueOnCapturedContext: false );
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( Properties.Resources.GetString( "MqttChannel_StreamDisconnected" ), disposedEx );
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
                _tracer.Info( Properties.Resources.GetString( "Mqtt_Disposing" ), GetType().FullName );

                _streamSubscription.Dispose();
                _receiver.OnCompleted();

                try
                {
                    _client?.Dispose();
                }
                catch( SocketException socketEx )
                {
                    _tracer.Error( socketEx, Properties.Resources.GetString( "MqttChannel_DisposeError" ), socketEx.SocketErrorCode );
                }

                _disposed = true;
            }
        }

        IDisposable SubscribeStream()
        {
            return Observable.Defer( () =>
            {
                byte[] buffer = new byte[_client.ReceiveBufferSize];

                return Observable.FromAsync<int>( () =>
                {
                    return _client.GetStream().ReadAsync( buffer, 0, buffer.Length );
                } )
                .Select( x => buffer.Take( x ) );
            } )
            .Repeat()
            .TakeWhile( bytes => bytes.Any() )
            .ObserveOn( NewThreadScheduler.Default )
            .Subscribe( bytes =>
            {

                if( _buffer.TryGetPackets( bytes, out IEnumerable<byte[]> packets ) )
                {
                    foreach( byte[] packet in packets )
                    {
                        _tracer.Verbose( Properties.Resources.GetString( "MqttChannel_ReceivedPacket" ), packet.Length );

                        _receiver.OnNext( packet );
                    }
                }
            }, ex =>
            {
                if( ex is ObjectDisposedException )
                {
                    _receiver.OnError( new MqttException( Properties.Resources.GetString( "MqttChannel_StreamDisconnected" ), ex ) );
                }
                else
                {
                    _receiver.OnError( ex );
                }
            }, () =>
            {
                _tracer.Warn( Properties.Resources.GetString( "MqttChannel_NetworkStreamCompleted" ) );
                _receiver.OnCompleted();
            } );
        }
    }
}
