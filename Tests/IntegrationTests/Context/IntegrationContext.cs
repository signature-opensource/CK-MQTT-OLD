using System.Diagnostics;
using System.Net;
using CK.MQTT;
using System.Threading.Tasks;
using CK.MQTT.Ssl;
using CK.MQTT.Sdk.Bindings;
using System;
using NUnit.Framework;

namespace IntegrationTests.Context
{
    public abstract class IntegrationContext : IDisposable
    {
        protected readonly ushort KeepAliveSecs;
        protected readonly bool AllowWildcardsInTopicFilters;

        static IntegrationContext()
        {
            Tracer.Configuration.AddListener( "CK.MQTT", new TestTracerListener() );
            Tracer.Configuration.SetTracingLevel( "CK.MQTT", SourceLevels.All );
        }

        protected IntegrationContext( ushort keepAliveSecs = 5, bool allowWildcardsInTopicFilters = true, IMqttAuthenticationProvider authenticationProvider = null )
        {
            KeepAliveSecs = keepAliveSecs;
            AllowWildcardsInTopicFilters = allowWildcardsInTopicFilters;
            AuthenticationProvider = authenticationProvider;
            Configuration = new MqttConfiguration
            {
                KeepAliveSecs = KeepAliveSecs,
                WaitTimeoutSecs = 5,
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                AllowWildcardsInTopicFilters = AllowWildcardsInTopicFilters
            };
        }

        [SetUp]
        public void Setup()
        {
            Server = MqttServer.Create( Configuration, MqttServerBinding, AuthenticationProvider );
            Server.Start();
        }

        protected MqttConfiguration Configuration { get; }

        protected abstract IMqttServerBinding MqttServerBinding { get; }

        protected abstract IMqttBinding MqttBinding { get; }

        protected virtual IMqttServer Server { get; private set; }

        protected IMqttAuthenticationProvider AuthenticationProvider { get; }

        protected virtual async Task<IMqttClient> GetClientAsync(string connectionString = null)
        {
            return await MqttClient.CreateAsync( connectionString ?? IPAddress.Loopback.ToString()+":25565", Configuration, MqttBinding );
        }

        [TearDown]
        public void Dispose()
        {
            Server.Dispose();
        }
    }
}
