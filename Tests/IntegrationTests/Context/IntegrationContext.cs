using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;

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
        protected virtual IRawTestClient GetRawTestClient()
        {
            LoadConfiguration();
            return new TcpRawTestClient( Configuration );
        }

        protected virtual async Task<IRawTestClient> GetRawConnectedTestClient()
        {
            var client = GetRawTestClient();
            var ssl = new SslStream(
                client.Stream,
                false,
                (a,b,c,d) =>true,
                null,
                EncryptionPolicy.RequireEncryption );
            await ssl.AuthenticateAsClientAsync( "127.0.0.1" );
            client.Stream = ssl;
            return client;
        }

                var certLocation = "localhost.pfx";
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
