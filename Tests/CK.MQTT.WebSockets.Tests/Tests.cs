using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using NUnit.Framework;

namespace CK.MQTT.TCP.Tests
{
    [TestFixture]
    public class AuthenticationSpecWebSockets : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();
        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class ConnectionSpecWebSockets : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveWebSockets : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class PrivateClientSpecWebSockets : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class PublishingSpecWebSockets : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }

    [TestFixture]
    public class SubscriptionSpecWebSockets : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerWebSocketBinding();

        protected override IMqttBinding MqttBinding => new WebSocketBinding();
    }
}
