using CK.Core;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using CK.Testing;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests.Context
{
    public abstract class IntegrationContext : IDisposable
    {
        protected readonly ushort KeepAliveSecs;
        protected readonly bool AllowWildcardsInTopicFilters;
        private readonly IMqttAuthenticationProvider _authenticationProvider;

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
            TestHelper.Monitor.Info("Starting tests !");
            var m = new ActivityMonitor( "Server Monitor." );
            Server = MqttServer.Create( m, Configuration, MqttServerBinding, _authenticationProvider ) ;
            Server.Start();
        }

        protected abstract IRawTestClient GetRawTestClient();

        protected abstract Task<IRawTestClient> GetRawConnectedTestClient();

        protected MqttConfiguration Configuration { get; }

        protected abstract IMqttServerBinding MqttServerBinding { get; }

        protected abstract IMqttBinding MqttBinding { get; }

        protected IMqttServer Server { get; private set; }

        int _i = 0;
        protected virtual async Task<IMqttClient> GetClientAsync()
        {
            var m = new ActivityMonitor( "Test Client Monitor " + _i++ );
            return await MqttClient.CreateAsync( m, IPAddress.Loopback.ToString(), Configuration, MqttBinding );
        }

        [TearDown]
        public void Dispose()
        {
            Server.Dispose();
        }
    }
}
