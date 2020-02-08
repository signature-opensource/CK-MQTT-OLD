using CK.Core;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelListener : IMqttChannelListener
    {
        readonly MqttConfiguration _configuration;
        readonly Lazy<TcpListener> _listener;
        bool disposed;

        public TcpChannelListener( IActivityMonitor m, int port, MqttConfiguration configuration )
        {//TODO: too much side effects, do this in a constructor.
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
                    m.Error( ServerProperties.TcpChannelProvider_TcpListener_Failed, socketEx );

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
                .Select<TcpClient, (IActivityMonitor monitor, TcpClient client)>( s => (new ActivityMonitor(), s) )
                .Repeat()
                .Select( ( s ) => new TcpChannel( s.monitor, s.client, new PacketBuffer(), _configuration ) );
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
