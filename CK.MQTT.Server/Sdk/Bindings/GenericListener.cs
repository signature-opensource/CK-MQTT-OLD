using CK.Core;
using CK.MQTT.Sdk;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    public class GenericListener<TChannel> : IMqttChannelListener
        where TChannel : IMqttChannel<byte[]>
    {
        readonly MqttConfiguration _configuration;
        /// <summary>
        /// A lazy initialized listener. Start the listener at the initialisation.
        /// </summary>
        readonly Lazy<IListener<TChannel>> _listener;
        bool _disposed;

        public GenericListener( IActivityMonitor m, MqttConfiguration configuration, Func<MqttConfiguration, IListener<TChannel>> listenerFactory )
        {
            _configuration = configuration;
            _listener = new Lazy<IListener<TChannel>>( () =>
            {
                IListener<TChannel> listener = listenerFactory( _configuration );

                try
                {
                    listener.Start();
                }
                catch( SocketException socketEx )
                {
                    m.Error( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );

                    throw new MqttException( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
                }

                return listener;
            } );
        }

        public IObservable<IMqttChannel<byte[]>> GetChannelStream()
        {
            var m = new ActivityMonitor();
            if( _disposed )
            {
                throw new ObjectDisposedException( GetType().FullName );
            }

            return Observable
                .FromAsync( () => SafeAcceptClient( m ) )
                .Repeat()
                .Select( client => (IMqttChannel<byte[]>)client );
        }
        async Task<TChannel> SafeAcceptClient( IActivityMonitor m )
        {
            while( true )
            {
                try
                {
                    return await _listener.Value.AcceptClientAsync( m );
                }
                catch( Exception e )
                {
                    m.Warn( "Error while trying to accept a client.", e );
                }
            }
        }

        public void Dispose()
        {
            if( _disposed ) return;

            _listener.Value.Stop();
            _disposed = true;
        }
    }
}
