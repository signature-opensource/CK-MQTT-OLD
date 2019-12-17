using CK.MQTT;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
	public abstract class ConnectedContext : IntegrationContext
	{
		protected ConnectedContext (ushort keepAliveSecs = 5, bool allowWildcardsInTopicFilters = true)
			: base (keepAliveSecs, allowWildcardsInTopicFilters)
		{
		}
		public bool CleanSession { get; set; }

		protected override async Task<IMqttClient> GetClientAsync ()
		{
			var client = await base.GetClientAsync ();
			await client.ConnectAsync (new MqttClientCredentials ( MqttTestHelper.GetClientId ()), cleanSession: CleanSession);
			return client;
		}
	}
}
