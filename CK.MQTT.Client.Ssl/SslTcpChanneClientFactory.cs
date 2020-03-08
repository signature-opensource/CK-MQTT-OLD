using CK.Core;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    public class SslTcpChanneClientFactory
    {
        readonly SslTcpConfig _config;
        private readonly MqttConfiguration _mqttConfig;
        readonly string _hostName;

        public SslTcpChanneClientFactory( SslTcpConfig config, MqttConfiguration mqqtConfig, string hostName )
        {
            _config = config;
            _mqttConfig = mqqtConfig;
            _hostName = hostName;
        }

        public async Task<IChannelClient> CreateAsync( IActivityMonitor m )
        {
            TimeSpan timeout = TimeSpan.FromSeconds( _mqttConfig.ConnectionTimeoutSecs );
            TcpClient client = new TcpClient( _config.AddressFamily );
            Task connectTask = client.ConnectAsync( _hostName, _mqttConfig.Port );
            Task connectTimeout = Task.Delay( timeout );
            await Task.WhenAny( connectTask, connectTimeout );
            if( !connectTask.IsCompleted )
            {
                throw new TimeoutException( "Server did not respond in the given time." );
            }
            if( connectTask.IsFaulted )
            {
                ExceptionDispatchInfo.Capture( connectTask.Exception.InnerException ).Throw();
            }
            SslStream ssl = new SslStream(
                client.GetStream(),
                false,
                _config.RemoteCertificateValidationCallback,
                _config.LocalCertificateSelectionCallback,
                EncryptionPolicy.RequireEncryption );
            Task authTask = ssl.AuthenticateAsClientAsync( _hostName );
            Task authTimeout = Task.Delay( timeout );
            await Task.WhenAny( authTask, authTimeout );
            if( !authTask.IsCompleted )
            {
                throw new TimeoutException( "Could not authenticate with the server in the given time." );
            }
            if( authTask.IsFaulted )
            {
                ExceptionDispatchInfo.Capture( connectTask.Exception.InnerException ).Throw();
            }
            return new SslTcpChannelClient( client, ssl );
        }

        public static IMqttChannelFactory MqttChannelFactory( string hostName, SslTcpConfig sslConfig, MqttConfiguration mqttConfig )
        {
            return new GenericChannelFactory( new SslTcpChanneClientFactory( sslConfig, mqttConfig, hostName ).CreateAsync, mqttConfig );
        }
    }

    public class SslTcpConfig
    {
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

        public AddressFamily AddressFamily { get; set; } = AddressFamily.InterNetwork;
    }
}
