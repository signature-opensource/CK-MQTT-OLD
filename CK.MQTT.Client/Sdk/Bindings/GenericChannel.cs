using CK.Core;

using CK.MQTT.Sdk;
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
    public sealed class GenericChannel : IMqttChannel<byte[]>
    {
        bool _disposed;
        readonly IChannelClient _client;
        readonly IPacketBuffer _buffer;
        readonly ReplaySubject<Mon<byte[]>> _receiver;
        readonly ReplaySubject<Mon<byte[]>> _sender;
        readonly IDisposable _streamSubscription;

        public GenericChannel(
			IChannelClient client,
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
            _client = client;
            _client.PreferedReceiveBufferSize = configuration.BufferSize;
            _client.PreferedSendBufferSize = configuration.BufferSize;
            _buffer = buffer;
            _receiver = new ReplaySubject<Mon<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
            _sender = new ReplaySubject<Mon<byte[]>>( window: TimeSpan.FromSeconds( configuration.WaitTimeoutSecs ) );
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

        public IObservable<Mon<byte[]>> ReceiverStream => _receiver;

        public IObservable<Mon<byte[]>> SenderStream => _sender;

        public async Task SendAsync( Mon<byte[]> message )
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
                message.Monitor.Trace( $"Sending packet of {message.Item.Length} bytes" );

                await _client.GetStream()
                    .WriteAsync( message.Item, 0, message.Item.Length );
            }
            catch( ObjectDisposedException disposedEx )
            {
                throw new MqttException( "The underlying communication stream is not available. The socket could have been disconnected", disposedEx );
            }
        }

        public void Dispose()
        {
            if( _disposed ) return;
            _streamSubscription.Dispose();
            _receiver.OnCompleted();

            try
            {
                _client?.Dispose();
            }
            catch( SocketException socketEx )
            {//Should have Start/Stop
                var m = new ActivityMonitor();
                m.Error( $"An error occurred while closing underlying communication channel. Error code: {socketEx.SocketErrorCode}", socketEx );
            }

            _disposed = true;
        }

        IDisposable SubscribeStream()
        {
            IActivityMonitor m = new ActivityMonitor();
            return Observable.Defer( () =>
            {
                byte[] buffer = new byte[_client.PreferedReceiveBufferSize];

                return Observable.FromAsync( () =>
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
                try
                {

                    if( _buffer.TryGetPackets( bytes, out IEnumerable<byte[]> packets ) )
                    {
                        foreach( var packet in packets )
                        {
                            using( m.OpenTrace( $"Received packet of {packet.Length} bytes" ) )
                            {
                                _receiver.OnNext( new Mon<byte[]>( m, packet ) );
                            }
                        }
                    }
                }
                catch( Exception e )
                {
                    try
                    {
                        m.Warn( "Error while processing client data. Closing connection.", e );
                        _client.Dispose();
                    }
                    catch( Exception F )
                    {
                        m.Warn(  "Error while disposing client connection.", F );
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
                var monitor = new ActivityMonitor();//TODO: remove OnComplete monitor.
                monitor.Warn( "The underlying communication stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed" );
                _receiver.OnCompleted();
            } );
        }
    }
}
