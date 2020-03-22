using CK.Core;
using CK.Monitoring;
using CK.Monitoring.Handlers;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            ActivityMonitor.AutoConfiguration += m => m.AutoTags = m.AutoTags.Union( ActivityMonitor.Tags.StackTrace );
            var conf = new GrandOutputConfiguration();
            conf.AddHandler( new TextFileConfiguration
            {
                Path = FileUtil.CreateUniqueTimedFolder( LogFile.RootLogPath + "Text/", null, DateTime.UtcNow ),
                AutoFlushRate = 0
            } );
            //GrandOutput.Default.ApplyConfiguration()
            TestHelper.Monitor.Info( "Starting tests !" );
            var m = new ActivityMonitor( "Server Monitor." );
            Server = MqttServer.Create( m, Configuration, MqttServerBinding, _authenticationProvider );
            Server.Start();
        }

        protected abstract IRawTestClient GetRawTestClient();

        protected abstract Task<IRawTestClient> GetRawConnectedTestClient();

        protected MqttConfiguration Configuration { get; }

        protected abstract IMqttServerBinding MqttServerBinding { get; }

        protected abstract IMqttBinding MqttBinding { get; }

        protected IMqttServer Server { get; private set; }

        int _i = 0;
        protected virtual async Task<(IMqttClient, IActivityMonitor m)> GetClientAsync( string name = null )
        {
            ActivityMonitor m = new ActivityMonitor( name ?? ("Test Client Monitor " + _i++) );
            return (await MqttClient.CreateAsync( m, IPAddress.Loopback.ToString(), Configuration, MqttBinding ), m);
        }

        [TearDown]
        public void Dispose()
        {
            TestHelper.Monitor.Info( "IntegrationContext TearDown." );
            Server.Dispose();
        }
    }
}
