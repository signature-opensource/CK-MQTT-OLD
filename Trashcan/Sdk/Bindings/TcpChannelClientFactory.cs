using CK.Core;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelClientFactory
    {
        readonly string _hostAddress;
        readonly MqttConfiguration _configuration;

        public TcpChannelClientFactory( string hostAddress, MqttConfiguration configuration )
        {
            _hostAddress = hostAddress;
            _configuration = configuration;
        }

        public async Task<IChannelClient> CreateAsync(IActivityMonitor m)
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                Task connectTask = tcpClient.ConnectAsync( _hostAddress, _configuration.Port );
                Task timeoutTask = Task.Delay( TimeSpan.FromSeconds( _configuration.ConnectionTimeoutSecs ) );
                Task resultTask = await Task.WhenAny( connectTask, timeoutTask );

                if( !connectTask.IsCompleted )
                {
                    throw new TimeoutException();
                }
                if( connectTask.IsFaulted )
                {
                    ExceptionDispatchInfo.Capture( resultTask.Exception.InnerException ).Throw();
                }
                return new TcpChannelClient( tcpClient );
            }
            catch( SocketException socketEx )
            {
                string message = ClientProperties.TcpChannelFactory_TcpClient_Failed( _hostAddress, _configuration.Port );

                m.Error( message, socketEx );

                throw new MqttException( message, socketEx );
            }
            catch( TimeoutException timeoutEx )
            {
                try
                {
                    // Just in case the connection is a little late,
                    // dispose the tcpClient. This may throw an exception,
                    // which we should just eat.
                    tcpClient.Dispose();
                }
                catch { }

                string message = ClientProperties.TcpChannelFactory_TcpClient_Failed( _hostAddress, _configuration.Port );

                m.Error( message, timeoutEx );
                throw new MqttException( message, timeoutEx );
            }
        }

        public static IMqttChannelFactory MqttChannelFactory( string hostName, MqttConfiguration config )
            => new GenericChannelFactory( new TcpChannelClientFactory( hostName, config ).CreateAsync, config );
    }
}
