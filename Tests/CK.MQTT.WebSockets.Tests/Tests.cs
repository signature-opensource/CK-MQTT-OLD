using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using NUnit.Framework;

namespace CK.MQTT.TCP.Tests
{
    [TestFixture]
    public class AuthenticationSpecSslTcp : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();
        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class ConnectionSpecSslTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveSslTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class PrivateClientSpecSslTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class PublishingSpecSslTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class SubscriptionSpecSslTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }
}
