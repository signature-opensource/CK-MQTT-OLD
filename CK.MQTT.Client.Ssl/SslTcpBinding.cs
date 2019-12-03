using System;
using System.Collections.Generic;
using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Bindings;
using System.Text;

namespace CK.MQTT.Ssl
{
	class SslTcpBinding : IMqttBinding
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
