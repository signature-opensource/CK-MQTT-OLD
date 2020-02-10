using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelFactory : IMqttChannelFactory
    {
        static readonly ITracer _tracer = Tracer.Get<TcpChannelFactory>();

        readonly string _hostAddress;
        readonly MqttConfiguration _configuration;

        public TcpChannelFactory( string hostAddress, MqttConfiguration configuration )
        {
            _hostAddress = hostAddress;
            _configuration = configuration;
        }

        public async Task<IMqttChannel<byte[]>> CreateAsync()
        {
            TcpClient tcpClient = new TcpClient();

            try
            {
                Task connectTask = tcpClient.ConnectAsync( _hostAddress, _configuration.Port );
                Task timeoutTask = Task.Delay( TimeSpan.FromSeconds( _configuration.ConnectionTimeoutSecs ) );
                Task resultTask = await Task
                    .WhenAny( connectTask, timeoutTask )
                    .ConfigureAwait( continueOnCapturedContext: false );

                if( resultTask == timeoutTask )
                    throw new TimeoutException();

                if( resultTask.IsFaulted )
                    ExceptionDispatchInfo.Capture( resultTask.Exception.InnerException ).Throw();

                return new TcpChannel( tcpClient, new PacketBuffer(), _configuration );
            }
            catch( SocketException socketEx )
            {
                string message = string.Format( Properties.Resources.GetString( "TcpChannelFactory_TcpClient_Failed" ), _hostAddress, _configuration.Port );

                _tracer.Error( socketEx, message );

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

                string message = string.Format( Properties.Resources.GetString( "TcpChannelFactory_TcpClient_Failed" ), _hostAddress, _configuration.Port );

                _tracer.Error( timeoutEx, message );

                throw new MqttException( message, timeoutEx );
            }
        }
    }
}
