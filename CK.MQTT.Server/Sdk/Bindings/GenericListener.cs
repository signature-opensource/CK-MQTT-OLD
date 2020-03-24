using CK.Core;
using CK.MQTT.Sdk;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
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
        readonly ReplaySubject<Mon<IMqttChannel<byte[]>>> _channelStream;
        readonly Task _backgroundTask;
        public GenericListener( MqttConfiguration configuration, Func<MqttConfiguration, IListener<TChannel>> listenerFactory )
        {
            _configuration = configuration;
            _channelStream = new ReplaySubject<Mon<IMqttChannel<byte[]>>>();
            _listenerFactory = listenerFactory ?? throw new ArgumentNullException( nameof( listenerFactory ) );
            _backgroundTask = BackgroundProcess();
        }

        public IObservable<Mon<IMqttChannel<byte[]>>> ChannelStream => _channelStream;

        /// <summary>
        /// return false if disposed.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        void InitListener( IActivityMonitor m )
        {
            lock( _initLock )
            {
                if( _disposed ) return;
                if( _listener != null ) throw new InvalidOperationException();
                _listener = _listenerFactory( _configuration );
            }
            try
            {
                _listener.Start();
            }
            catch( SocketException socketEx )
            {
                m.Error( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
                throw new MqttException( ClientProperties.TcpChannelProvider_TcpListener_Failed, socketEx );
            }
        }

        async Task BackgroundProcess()
        {
            var m = new ActivityMonitor();
            InitListener( m );
            while( !_disposed )
            {
                try
                {
                    var channel = await _listener.AcceptClientAsync( m );
                    _channelStream.OnNext( new Mon<IMqttChannel<byte[]>>( m, channel ) );
                }
                catch( ObjectDisposedException )
                {
                    if( _disposed ) break;
                    throw;
                }
                catch( Exception e )
                {
                    m.Warn( "Error while accetping client: ", e );
                }
            }
            m.Trace( "Listener diposed, stopping background listener." );
            _channelStream.OnCompleted();
        }

        public void Dispose()
        {
            if( _disposed ) return;
            _disposed = true;
            //TODO: This logic should be in a Start/Stop logic.

            lock( _initLock )
            {
                _listener?.Stop();
            }
            if( !_backgroundTask.IsCompleted ) _backgroundTask.Wait( 50 );
            if( !_backgroundTask.IsCompleted )
            {
                var m = new ActivityMonitor();
                m.Warn( "Background task did not completed in given time." );
            }
            if( _backgroundTask.IsFaulted )
            {
                throw _backgroundTask.Exception;
            }
        }
    }
}
