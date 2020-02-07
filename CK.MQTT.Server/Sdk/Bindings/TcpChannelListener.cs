using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelListener : IMqttChannelListener
    {
        static readonly ITracer _tracer = Tracer.Get<TcpChannelListener>();

        readonly MqttConfiguration _configuration;
        readonly Lazy<TcpListener> _listener;
        bool disposed;

        public TcpChannelListener( int port, MqttConfiguration configuration )
        {
            _configuration = configuration;
            _listener = new Lazy<TcpListener>( () =>
            {
                if( disposed ) return null;
                var tcpListener = new TcpListener( IPAddress.Any, port );

                try
                {
                    tcpListener.Start();
                }
                catch( SocketException socketEx )
                {
                    _tracer.Error( socketEx, ServerProperties.TcpChannelProvider_TcpListener_Failed );

                    throw new MqttException( ServerProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
                }

                return tcpListener;
            } );
        }

        public IObservable<IMqttChannel<byte[]>> GetChannelStream()
        {
            if( disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return Observable
                .FromAsync( _listener.Value.AcceptTcpClientAsync )
                .Repeat()
                .Select( client => new TcpChannel( client, new PacketBuffer(), _configuration ) );
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }

        protected virtual void Dispose( bool disposing )
        {
            if( disposed ) return;

            if( disposing )
            {
                disposed = true;
                _listener.Value?.Stop();
            }
        }
    }
}
