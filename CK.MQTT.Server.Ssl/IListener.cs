using CK.MQTT.Sdk;
using System.Threading.Tasks;

namespace CK.MQTT.Ssl
{
	public interface IListener<TChannel> where TChannel : IMqttChannel<byte[]>
	{
		void Start();

		void Stop();

        Task<TChannel> AcceptClientAsync();
	}
}
