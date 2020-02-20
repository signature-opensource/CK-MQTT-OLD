using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using CK.MQTT;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using CK.MQTT.Ssl;
using static CK.Testing.BasicTestHelper;
namespace IntegrationTests.Context
{
    public abstract class IntegrationContext
    {
        protected readonly ushort KeepAliveSecs;
        protected readonly bool AllowWildcardsInTopicFilters;

        static IntegrationContext()
        {
            Tracer.Configuration.AddListener( "CK.MQTT", new TestTracerListener() );
            Tracer.Configuration.SetTracingLevel( "CK.MQTT", SourceLevels.All );
        }

        public IntegrationContext( ushort keepAliveSecs = 0, bool allowWildcardsInTopicFilters = true )
        {
            this.KeepAliveSecs = keepAliveSecs;
            this.AllowWildcardsInTopicFilters = allowWildcardsInTopicFilters;
        }

        protected MqttConfiguration Configuration { get; private set; }

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

        protected async Task<IMqttServer> GetServerAsync( IMqttAuthenticationProvider authenticationProvider = null )
        {
            try
            {
                LoadConfiguration();
                var config = new SslTcpConfig()
                {
                    AddressFamily = AddressFamily.InterNetwork,
                    
                };
                X509Certificate2Collection collection = new X509Certificate2Collection();
                var certLocation = "localhost.pfx";
                collection.Import( certLocation );
                X509Certificate2 certificate = null;
                foreach( X509Certificate2 singleHack in collection )
                {
                    certificate = singleHack;
                    break;
                }
                if( certificate == null ) throw new InvalidOperationException();
                var sslServerConfig = new ServerSslConfig
                {
                    ServerCertificate = certificate,
                    SslProtocols = SslProtocols.Tls12
                };
                var server = MqttServer.Create( Configuration, new ServerTcpSslBinding(config, sslServerConfig ), authenticationProvider );
                server.Start();
                return server;
            }
            catch( MqttException protocolEx )
            {
                if( protocolEx.InnerException is SocketException )
                {
                    return await GetServerAsync();
                }
                else
                {
                    throw;
                }
            }
        }

        protected virtual async Task<IMqttClient> GetClientAsync()
        {
            LoadConfiguration();
            SslTcpConfig sslConfig = new SslTcpConfig()
            {
                RemoteCertificateValidationCallback = ( s, c, ch, ssl ) => true
            };
            return await MqttClient.CreateAsync( IPAddress.Loopback.ToString(), Configuration, new SslTcpBinding( sslConfig ) );
        }

        protected string GetClientId()
        {
            return string.Concat( "Client", Guid.NewGuid().ToString().Replace( "-", string.Empty ).Substring( 0, 15 ) );
        }

        void LoadConfiguration()
        {
            if( Configuration != null ) return;
            Configuration = new MqttConfiguration
            {
                BufferSize = 128 * 1024,
                Port = GetPort(),
                KeepAliveSecs = KeepAliveSecs,
                WaitTimeoutSecs = 5,
                MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
                AllowWildcardsInTopicFilters = AllowWildcardsInTopicFilters
            };
        }

        static int GetPort()
        {
            return 25565;
        }
    }
}
