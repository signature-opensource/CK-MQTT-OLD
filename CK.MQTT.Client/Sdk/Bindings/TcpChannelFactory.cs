using CK.Core;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Bindings
{
    internal class TcpChannelFactory : IMqttChannelFactory
    {
        readonly string _connectionString;
        readonly MqttConfiguration _configuration;

        readonly string _hostAddress;
        readonly int _port;
        readonly int? _bufferSize;
        public TcpChannelFactory( string connectionString, MqttConfiguration configuration )
        {

            _connectionString = connectionString;
            _configuration = configuration;
            var match = MqttConstants.ConnectionStringRegex.Match( connectionString );
            _hostAddress = match.Groups[match.Groups[1].Value != null ? 1 : 3].Value;
            _port = match.Groups[2].Value.Length == 0 ? MqttProtocol.DefaultNonSecurePort : int.Parse( match.Groups[2].Value );
            _bufferSize = match.Groups[4]?.Value.Length == 0 ? (int?)null : int.Parse( match.Groups[4]?.Value );
        }

        public async Task<IMqttChannel<byte[]>> CreateAsync( IActivityMonitor m )
        {
            var tcpClient = new TcpClient();
            if( _bufferSize.HasValue )
            {
                tcpClient.ReceiveBufferSize = _bufferSize.Value;
                tcpClient.SendBufferSize = _bufferSize.Value;
            }
            try
            {
                var connectTask = tcpClient.ConnectAsync( _hostAddress, _port );
                var timeoutTask = Task.Delay( TimeSpan.FromSeconds( _configuration.ConnectionTimeoutSecs ) );
                var resultTask = await Task
                    .WhenAny( connectTask, timeoutTask )
                    .ConfigureAwait( continueOnCapturedContext: false );

                if( resultTask == timeoutTask )
                    throw new TimeoutException();

                if( resultTask.IsFaulted )
                    ExceptionDispatchInfo.Capture( resultTask.Exception.InnerException ).Throw();

                return new TcpChannel( m, tcpClient, new PacketBuffer(), _configuration );
            }
            catch( SocketException socketEx )
            {
                var message = string.Format( Properties.TcpChannelFactory_TcpClient_Failed, _connectionString, _port );

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

                var message = string.Format( Properties.TcpChannelFactory_TcpClient_Failed, _connectionString, _port );

                m.Error( message, timeoutEx );

                throw new MqttException( message, timeoutEx );
            }
        }
    }
}
