using CK.MQTT.Sdk;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    class SslTcpChannelListener : IListener<GenericChannel>
    {
        readonly TcpListener _listener;
        readonly MqttConfiguration _configuration;
        readonly SslTcpConfig _sslConfig;
        readonly ServerSslConfig _sslServerConfig;

        public SslTcpChannelListener( MqttConfiguration configuration, SslTcpConfig sslConfig, ServerSslConfig sslServerConfig )
        {
            _listener = new TcpListener( IPAddress.Any, configuration.Port );
            _configuration = configuration;
            _sslConfig = sslConfig;
            _sslServerConfig = sslServerConfig;
        }

        public async Task<GenericChannel> AcceptClientAsync()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            SslStream ssl = new SslStream(
                client.GetStream(),
                false,
                _sslConfig.RemoteCertificateValidationCallback,
                _sslConfig.LocalCertificateSelectionCallback,
                EncryptionPolicy.RequireEncryption
            );
            await ssl.AuthenticateAsServerAsync(
                _sslServerConfig.ServerCertificate,
                _sslConfig.RemoteCertificateValidationCallback != null,
                _sslServerConfig.SslProtocols,
                true
            );
            return new GenericChannel( new SslTcpChannelClient( client, ssl ), new PacketBuffer(), _configuration );
        }

        public void Start() => _listener.Start();

        public void Stop() => _listener.Stop();



    }
}
