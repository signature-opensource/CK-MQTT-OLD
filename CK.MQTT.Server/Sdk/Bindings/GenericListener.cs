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
        readonly Func<MqttConfiguration, IListener<TChannel>> _listenerFactory;
        /// <summary>
        /// A lazy initialized listener. Start the listener at the initialisation.
        /// </summary>
        IListener<TChannel> _listener;
        bool _disposed;
        readonly object _initLock = new object();
        IObservable<Mon<IMqttChannel<byte[]>>> _channelStream;

        public GenericListener( MqttConfiguration configuration, Func<MqttConfiguration, IListener<TChannel>> listenerFactory )
        {
            _configuration = configuration;
            _listenerFactory = listenerFactory ?? throw new ArgumentNullException( nameof( listenerFactory ) );
        }

        /// <summary>
        /// return false if disposed.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        bool InitListener( IActivityMonitor m )
        {
            IListener<TChannel> listener;
            lock( _initLock )
            {
                if( _disposed ) return false;
                if( _listener != null ) return true;
                listener = _listenerFactory( _configuration );
            }
            try
            {
                listener.Start();
                return true;
            }
            catch( SocketException socketEx )
            {
                m.Error( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
                throw new MqttException( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
            }
        }


        public IObservable<Mon<IMqttChannel<byte[]>>> ChannelStream
        {
            get
            {
                if( _disposed ) throw new ObjectDisposedException( GetType().FullName );
                if( _channelStream == null )
                {
                    _channelStream = Observable
                        .FromAsync( SafeAcceptClient )
                        .Repeat()
                        .Select( client => new Mon<IMqttChannel<byte[]>>( client.Monitor, client.Item ) );
                }

                return _channelStream;
            }
        }
        async Task<Mon<TChannel>> SafeAcceptClient()
        {
            var m = new ActivityMonitor();
            while( true )
            {
                if( _disposed ) return default;
                if( _listener == null )
                {
                    if( !InitListener( m ) ) return default;
                }
                try
                {

                    return new Mon<TChannel>( m, await _listener.AcceptClientAsync( m ) );
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
            _disposed = true;
            lock( _initLock )
            {
                _listener?.Stop();
            }
        }
    }
}
