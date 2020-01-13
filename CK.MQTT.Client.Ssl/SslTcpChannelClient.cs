using System.IO;
using System.Net.Security;
using System.Net.Sockets;

namespace CK.MQTT.Ssl
{
	public class SslTcpChannelClient : IChannelClient
	{
		readonly TcpClient _tcpClient;
		readonly SslStream _sslStream;

		public SslTcpChannelClient(TcpClient tcpClient, SslStream sslStream)
		{
			_tcpClient = tcpClient;
			_sslStream = sslStream;
		}
		public bool Connected => _tcpClient.Connected;

		public int PreferedSendBufferSize { get => _tcpClient.SendBufferSize; set => _tcpClient.SendBufferSize = value; }
		public int PreferedReceiveBufferSize { get => _tcpClient.ReceiveBufferSize; set => _tcpClient.ReceiveBufferSize = value; }

        public bool IsThreadSafe => false;

        public void Dispose()
		{
			_sslStream.Dispose();
			_tcpClient.Dispose();
		}

		public Stream Stream => _sslStream;
	}
}
