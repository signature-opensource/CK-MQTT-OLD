using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Ssl;
using IntegrationTests;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using static CK.Testing.BasicTestHelper;

namespace CK.MQTT.SslTcp.Tests
{
    public static class SslTcpHelper
    {
        public static IMqttServerBinding MqttServerBinding
        {
            get
            {
                SslTcpConfig config = new SslTcpConfig()
                {
                    AddressFamily = AddressFamily.InterNetwork,
                };
                X509Certificate2Collection collection = new X509Certificate2Collection();
                Text.NormalizedPath certLocation = TestHelper.TestProjectFolder.AppendPart( "localhost.pfx" );
                collection.Import( certLocation );
                X509Certificate2 certificate = null;
                foreach( X509Certificate2 singleHack in collection )
                {
                    certificate = singleHack;
                    break;
                }
                if( certificate == null ) throw new InvalidOperationException();
                ServerSslConfig sslServerConfig = new ServerSslConfig
                {
                    ServerCertificate = certificate,
                    SslProtocols = SslProtocols.Tls12
                };
                return new ServerTcpSslBinding( config, sslServerConfig );
            }
        }

        public static IMqttBinding MqttBinding => new SslTcpBinding( new SslTcpConfig()
        {
            RemoteCertificateValidationCallback = ( s, c, ch, ssl ) => true
        } );

        public static IRawTestClient GetRawTestClient( MqttConfiguration config )
        {
            return new TcpRawTestClient( config );
        }

        public static async Task<IRawTestClient> ConnectedRawClient( MqttConfiguration config )
        {
            var client = GetRawTestClient( config );
            var ssl = new SslStream(
                client.Stream,
                false,
                ( a, b, c, d ) => true,
                null,
                EncryptionPolicy.RequireEncryption );
            await ssl.AuthenticateAsClientAsync( "127.0.0.1" );
            client.Stream = ssl;
            return client;
        }
    }

    [TestFixture]
    public class AuthenticationSpecSslTcp : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;

        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class ConnectionSpecSslTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveSslTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class PrivateClientSpecSslTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class PublishingSpecSslTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class SubscriptionSpecSslTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => SslTcpHelper.MqttServerBinding;

        protected override IMqttBinding MqttBinding => SslTcpHelper.MqttBinding;
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => SslTcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => SslTcpHelper.GetRawTestClient( Configuration );
    }
}
