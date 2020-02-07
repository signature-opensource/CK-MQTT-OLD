using System;
using System.Threading.Tasks;
using IntegrationTests.Context;
using CK.MQTT;
using FluentAssertions;
using NUnit.Framework;

namespace IntegrationTests
{

    public abstract class AuthenticationSpec : IntegrationContext
    {
         protected AuthenticationSpec() : base(authenticationProvider: new TestAuthenticationProvider( expectedUsername: "foo", expectedPassword: "foo123" ) )
         {
         }

        [Test]
        public async Task when_client_connects_with_invalid_credentials_and_authentication_is_supported_then_connection_is_closed()
        {
            var username = "foo";
            var password = "foo123456";
            var client = await GetClientAsync();

            var aggregateEx = Assert.Throws<AggregateException>( () => client.ConnectAsync( new MqttClientCredentials( MqttTestHelper.GetClientId(), username, password ) ).Wait() );

            Assert.NotNull( aggregateEx.InnerException );
            Assert.True( aggregateEx.InnerException is MqttClientException );
            Assert.NotNull( aggregateEx.InnerException.InnerException );
            Assert.True( aggregateEx.InnerException.InnerException is MqttConnectionException );

            ((MqttConnectionException)aggregateEx.InnerException.InnerException).ReturnCode.Should().Be( MqttConnectionStatus.BadUserNameOrPassword );
        }

        [Test]
        public async Task when_client_connects_with_valid_credentials_and_authentication_is_supported_then_connection_succeeds()
        {
            var username = "foo";
            var password = "foo123";
            var client = await GetClientAsync();

            await client.ConnectAsync( new MqttClientCredentials( MqttTestHelper.GetClientId(), username, password ) );

            Assert.True( client.IsConnected );
            Assert.False( string.IsNullOrEmpty( client.Id ) );
        }
    }
}
