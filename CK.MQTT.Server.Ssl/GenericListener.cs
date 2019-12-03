using CK.MQTT.Sdk;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;

namespace CK.MQTT.Ssl
{
    public class GenericListener<TChannel> : IMqttChannelListener
        where TChannel : IMqttChannel<byte[]>
    {
        static readonly ITracer _tracer = Tracer.Get<GenericListener<TChannel>>();

        readonly MqttConfiguration _configuration;
        /// <summary>
        /// A lazy initialized listener. Start the listener at the initialisation.
        /// </summary>
        readonly Lazy<IListener<TChannel>> _listener;
        bool _disposed;

        public GenericListener( MqttConfiguration configuration, Func<MqttConfiguration, IListener<TChannel>> listenerFactory )
        {
            _configuration = configuration;
            _listener = new Lazy<IListener<TChannel>>( () =>
            {
                var tcpListener = listenerFactory( _configuration);

                try
                {
                    tcpListener.Start();
                }
                catch( SocketException socketEx )
                {
                    _tracer.Error( socketEx, Properties.Resources.GetString( "TcpChannelProvider_TcpListener_Failed" ) );

                    throw new MqttException( Properties.Resources.GetString( "TcpChannelProvider_TcpListener_Failed" ), socketEx );
                }

                return tcpListener;
            } );
        }

        public IObservable<IMqttChannel<byte[]>> GetChannelStream()
        {
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return Observable
                .FromAsync( _listener.Value.AcceptClientAsync )
                .Repeat()
                .Select( client => (IMqttChannel<byte[]>) client);
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
                _listener.Value.Stop();
                _disposed = true;
            }
        }
    }
}
