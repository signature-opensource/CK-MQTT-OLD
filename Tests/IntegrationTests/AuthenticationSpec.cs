using CK.Core;
using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{

    public abstract class AuthenticationSpec : IntegrationContext
    {
        protected AuthenticationSpec() : base( authenticationProvider: new TestAuthenticationProvider( expectedUsername: "foo", expectedPassword: "foo123" ) )
        {
        }

        [Test]
        public async Task when_client_connects_with_invalid_credentials_and_authentication_is_supported_then_connection_is_closed()
        {
            string username = "foo";
            string password = "foo123456";
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();

            AggregateException aggregateEx = Assert.Throws<AggregateException>( () => client.ConnectAsync( m, new MqttClientCredentials( MqttTestHelper.GetClientId(), username, password ) ).Wait() );

            Assert.NotNull( aggregateEx.InnerException );
            Assert.True( aggregateEx.InnerException is MqttClientException );
            Assert.NotNull( aggregateEx.InnerException.InnerException );
            Assert.True( aggregateEx.InnerException.InnerException is MqttConnectionException );

            ((MqttConnectionException)aggregateEx.InnerException.InnerException).ReturnCode.Should().Be( MqttConnectionStatus.BadUserNameOrPassword );
        }

        [Test]
        public async Task when_client_connects_with_valid_credentials_and_authentication_is_supported_then_connection_succeeds()
        {
            string username = "foo";
            string password = "foo123";
            (IMqttClient client, IActivityMonitor m) = await GetClientAsync();

            await client.ConnectAsync( m, new MqttClientCredentials( MqttTestHelper.GetClientId(), username, password ) );

            Assert.True( client.CheckConnection( m ) );
            Assert.False( string.IsNullOrEmpty( client.ClientId ) );
        }
    }
}
