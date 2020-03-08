using CK.Core;
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

        protected override async Task<(IMqttClient, IActivityMonitor)> GetClientAsync(string name = null)
        {
            var client = await base.GetClientAsync( name );
            await client.Item1.ConnectAsync(
                client.m,
                new MqttClientCredentials( MqttTestHelper.GetClientId() ), cleanSession: CleanSession
            );
            return client;
        }
    }
}
