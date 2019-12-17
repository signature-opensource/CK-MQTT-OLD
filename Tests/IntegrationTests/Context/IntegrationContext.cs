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
        private readonly IMqttAuthenticationProvider _authenticationProvider;

        static IntegrationContext()
        {
            Tracer.Configuration.AddListener( "CK.MQTT", new TestTracerListener() );
            Tracer.Configuration.SetTracingLevel( "CK.MQTT", SourceLevels.All );
        }

        protected IntegrationContext( ushort keepAliveSecs = 5, bool allowWildcardsInTopicFilters = true, IMqttAuthenticationProvider authenticationProvider = null )
        {
            KeepAliveSecs = keepAliveSecs;
            AllowWildcardsInTopicFilters = allowWildcardsInTopicFilters;
            _authenticationProvider = authenticationProvider;
            Configuration = new MqttConfiguration
            {
                BufferSize = 128 * 1024,
                Port = 25565,
                KeepAliveSecs = KeepAliveSecs,
                WaitTimeoutSecs = 5,
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                AllowWildcardsInTopicFilters = AllowWildcardsInTopicFilters
            };
        }

        [SetUp]
        public void Setup()
        {
            Server = MqttServer.Create( Configuration, MqttServerBinding, _authenticationProvider );
            Server.Start();
        }

        protected MqttConfiguration Configuration { get; }

        protected abstract IMqttServerBinding MqttServerBinding { get; }

        protected abstract IMqttBinding MqttBinding { get; }

        protected IMqttServer Server { get; private set; }

        protected virtual async Task<IMqttClient> GetClientAsync()
        {
            return await MqttClient.CreateAsync( IPAddress.Loopback.ToString(), Configuration, MqttBinding );
        }

        [TearDown]
        public void Dispose()
        {
            Server.Dispose();
        }
    }
}
