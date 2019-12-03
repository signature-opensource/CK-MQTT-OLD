using CK.MQTT.Sdk;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
    class SslTcpListener : IListener<GenericChannel>
    {
        readonly TcpListener _listener;
        readonly MqttConfiguration _configuration;
        private readonly SslTcpConfig _sslConfig;

        public SslTcpListener( MqttConfiguration configuration, SslTcpConfig sslConfig )
        {
            _listener = new TcpListener( IPAddress.Any, configuration.Port );
            _configuration = configuration;
            _sslConfig = sslConfig;
        }

        public async Task<GenericChannel> AcceptClientAsync()
        {
            TcpClient client = await _listener.AcceptTcpClientAsync();
            var ssl = new SslStream(
                client.GetStream(),
                false,
                _sslConfig.UserCertificateValidationCallback,
                _sslConfig.LocalCertificateSelectionCallback,
                EncryptionPolicy.RequireEncryption
            );
            return new GenericChannel( new SslTcpChannelClient( client, ssl ), new PacketBuffer(), _configuration );
        }

        public void Start() => _listener.Start();

        public void Stop() => _listener.Stop();
    }
}
