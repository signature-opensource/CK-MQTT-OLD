using CK.MQTT;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests.Context
{
    public abstract class ConnectedContext : IntegrationContext
    {
        protected ConnectedContext( ushort keepAliveSecs = 5, bool allowWildcardsInTopicFilters = true )
            : base( keepAliveSecs, allowWildcardsInTopicFilters )
        {
        }
        public bool CleanSession { get; set; }

        protected override async Task<IMqttClient> GetClientAsync()
        {
            IMqttClient client = await base.GetClientAsync();
            await client.ConnectAsync( TestHelper.Monitor, new MqttClientCredentials( MqttTestHelper.GetClientId() ), cleanSession: CleanSession );
            return client;
        }
    }
}
