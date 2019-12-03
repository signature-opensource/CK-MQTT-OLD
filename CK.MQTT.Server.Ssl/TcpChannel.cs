using System.Net.Sockets;
using CK.MQTT.Sdk;

namespace CK.MQTT.Ssl
{
	internal class TcpChannel
	{
		private TcpClient client;
		private PacketBuffer packetBuffer;
		private MqttConfiguration configuration;

		public TcpChannel(TcpClient client, PacketBuffer packetBuffer, MqttConfiguration configuration)
		{
			this.client = client;
			this.packetBuffer = packetBuffer;
			this.configuration = configuration;
		}
	}
}
