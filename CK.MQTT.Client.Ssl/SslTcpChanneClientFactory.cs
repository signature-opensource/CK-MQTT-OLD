using CK.MQTT.Sdk;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    public class SslTcpChanneClientFactory
    {
        readonly SslTcpConfig _config;
        readonly MqttConfiguration _mqqtConfig;
        readonly string _hostAddress;
        readonly int _port;
        readonly int? _bufferSize;
        public SslTcpChanneClientFactory( SslTcpConfig config, MqttConfiguration mqqtConfig, string connectionString )
        {
            _config = config;
            _mqqtConfig = mqqtConfig;
            var match = MqttConstants.ConnectionStringRegex.Match( connectionString );
            _hostAddress = match.Groups[match.Groups[1].Value != null ? 1 : 3].Value;
            _port = match.Groups[2].Value.Length == 0 ? MqttProtocol.DefaultNonSecurePort : int.Parse( match.Groups[2].Value );
            _bufferSize = match.Groups[4]?.Value.Length == 0 ? (int?)null : int.Parse( match.Groups[4]?.Value );
        }

        public async Task<IChannelClient> CreateAsync()
        {
            var client = new TcpClient( _config.AddressFamily );
            if(_bufferSize.HasValue)
            {
                client.ReceiveBufferSize = _bufferSize.Value;
                client.SendBufferSize = _bufferSize.Value;
            }
            
            await client.ConnectAsync( _hostAddress, _port );
            var ssl = new SslStream(
                client.GetStream(),
                false,
                _config.RemoteCertificateValidationCallback,
                _config.LocalCertificateSelectionCallback,
                EncryptionPolicy.RequireEncryption );
            await ssl.AuthenticateAsClientAsync( _hostAddress );
            return new SslTcpChannelClient( client, ssl );
        }

        public static IMqttChannelFactory MqttChannelFactory( string connectionString, SslTcpConfig sslConfig, MqttConfiguration mqttConfig )
        {
            return new GenericChannelFactory( new SslTcpChanneClientFactory( sslConfig, mqttConfig, connectionString ).CreateAsync, mqttConfig );
        }
    }

    public class SslTcpConfig
    {
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

        public AddressFamily AddressFamily { get; set; } = AddressFamily.InterNetwork;
    }
}
