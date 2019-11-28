using System;
using System.Collections.Generic;
using CK.MQTT;
using CK.MQTT.Sdk;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	public class SslTcpChanneClientFactory
	{
		readonly SslTcpConfig _config;
		readonly string _hostName;

		public SslTcpChanneClientFactory(SslTcpConfig config, string hostName)
		{
			_config = config;
			_hostName = hostName;
		}

		public async Task<IChannelClient> CreateAsync()
		{
			var client = new TcpClient(_config.AddressFamily);
			await client.ConnectAsync(_hostName, _config.Port);
			var ssl = new SslStream(
				client.GetStream(),
				false,
				_config.UserCertificateValidationCallback,
				_config.LocalCertificateSelectionCallback,
				EncryptionPolicy.RequireEncryption);
			await ssl.AuthenticateAsClientAsync(_hostName);
			return new SslTcpChannelClient(client, ssl);
		}

		public static IMqttChannelFactory MqttChannelFactory(string hostName, SslTcpConfig sslConfig, MqttConfiguration mqqtConfig)
		{
			return new GenericChannelFactory(new SslTcpChanneClientFactory(sslConfig, hostName).CreateAsync, mqqtConfig);
		}
	}

	public class SslTcpConfig
	{
		public int Port { get; set; }

		public RemoteCertificateValidationCallback UserCertificateValidationCallback { get; set; }

		public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

		public AddressFamily AddressFamily { get; set; } = AddressFamily.InterNetwork;
	}
}
