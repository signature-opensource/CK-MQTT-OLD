using CK.MQTT;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
    public abstract class ConnectedContext : IntegrationContext
    {
        protected ConnectedContext( ushort keepAliveSecs = 5, bool allowWildcardsInTopicFilters = true )
            : base( keepAliveSecs, allowWildcardsInTopicFilters )
        {
        }
        public bool CleanSession { get; set; }

        protected virtual async Task<IMqttClient> GetConnectedClientAsync( string connectionString = null )
        {
            var client = await GetClientAsync( connectionString );
            await client.ConnectAsync( new MqttClientCredentials( MqttTestHelper.GetClientId() ), cleanSession: CleanSession );
            return client;
        }
    }
}
