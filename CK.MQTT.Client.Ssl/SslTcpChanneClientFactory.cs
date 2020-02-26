using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    public class SslTcpChanneClientFactory
    {
        readonly SslTcpConfig _config;
        private readonly MqttConfiguration _mqqtConfig;
        readonly string _hostName;

        public SslTcpChanneClientFactory( SslTcpConfig config, MqttConfiguration mqqtConfig, string hostName )
        {
            _config = config;
            _mqqtConfig = mqqtConfig;
            _hostName = hostName;
        }

        public async Task<IChannelClient> CreateAsync()
        {
            TcpClient client = new TcpClient( _config.AddressFamily );
            await client.ConnectAsync( _hostName, _mqqtConfig.Port );
            SslStream ssl = new SslStream(
                client.GetStream(),
                false,
                _config.RemoteCertificateValidationCallback,
                _config.LocalCertificateSelectionCallback,
                EncryptionPolicy.RequireEncryption );
            await ssl.AuthenticateAsClientAsync( _hostName );
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
