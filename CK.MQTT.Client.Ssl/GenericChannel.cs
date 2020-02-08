using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.MQTT.Sdk;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    public sealed class GenericChannel : IMqttChannel<byte[]>
    {
        bool _disposed;

        readonly IChannelClient _client;
        readonly IPacketBuffer _buffer;
        readonly ReplaySubject<byte[]> _receiver;
        readonly ReplaySubject<byte[]> _sender;
        readonly IDisposable _streamSubscription;
        readonly object _streamLock = new object();
        public GenericChannel(
            IChannelClient client,
            IPacketBuffer buffer,
            MqttConfiguration configuration )
        {
            _client = client;
            _buffer = buffer;
            _receiver = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<byte[]>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
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

        public IObservable<byte[]> ReceiverStream => _receiver;

        public IObservable<byte[]> SenderStream => _sender;

        public Task SendAsync( byte[] message )
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            if( !IsConnected )
            {
                throw new MqttException( "The underlying communication stream is not connected" );
            }

            _sender.OnNext( message );

            try
            {
                _tracer.Verbose( "Sending packet of {0} bytes", message.Length );
                if( _client.IsThreadSafe )
                {
                    return _client.Stream.WriteAsync( message, 0, message.Length );
                }
                lock( _streamLock )
                {
                    _client.Stream.Write( message, 0, message.Length );
                }
                return Task.CompletedTask;
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( "The underlying communication stream is not available. The socket could have been disconnected", disposedEx );
            }
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        void Dispose( bool disposing )
        {
            if( _disposed ) return;

            if( disposing )
            {
                _tracer.Info( "Disposing {0}...", GetType().FullName );

                _streamSubscription.Dispose();
                _receiver.OnCompleted();

                try
                {
                    _client?.Dispose();
                }
                catch( SocketException socketEx )
                {
                    _tracer.Error( socketEx, "An error occurred while closing underlying communication channel. Error code: {0}", socketEx.SocketErrorCode );
                }

                _disposed = true;
            }
        }

        IDisposable SubscribeStream()
        {
            return Observable.Defer( () =>
            {
                var buffer = new byte[_client.PreferedReceiveBufferSize];

                return Observable.FromAsync<int>( () =>
                {
                    return _client.Stream.ReadAsync( buffer, 0, buffer.Length );
                } )
                .Select( x => buffer.Take( x ) );
            } )
            .Repeat()
            .TakeWhile( bytes => bytes.Any() )
            .ObserveOn( NewThreadScheduler.Default )
            .Subscribe( bytes =>
            {
                var packets = default( IEnumerable<byte[]> );

                if( _buffer.TryGetPackets( bytes, out packets ) )
                {
                    foreach( var packet in packets )
                    {
                        _tracer.Verbose( "Received packet of {0} bytes", packet.Length );

                        _receiver.OnNext( packet );
                    }
                }
            }, ex =>
            {
                if( ex is ObjectDisposedException )
                {
                    _receiver.OnError( new MqttException( "The underlying communication stream is not available. The socket could have been disconnected", ex ) );
                }
                else
                {
                    _receiver.OnError( ex );
                }
            }, () =>
            {
                _tracer.Warn( "The underlying communication stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed" );
                _receiver.OnCompleted();
            } );
        }
    }
}
