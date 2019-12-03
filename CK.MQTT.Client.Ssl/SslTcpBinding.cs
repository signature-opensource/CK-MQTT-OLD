using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;

namespace CK.MQTT.Ssl
{
	public class SslTcpBinding : IMqttBinding
	{
		readonly SslTcpConfig _sslConfig;

		public SslTcpBinding(SslTcpConfig sslConfig)
		{
			_sslConfig = sslConfig;
		}
		public IMqttChannelFactory GetChannelFactory(string hostAddress, MqttConfiguration configuration)
		{
			return SslTcpChanneClientFactory.MqttChannelFactory(hostAddress, _sslConfig, configuration);
		}
	}
}
