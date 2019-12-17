using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using NUnit.Framework;

namespace CK.MQTT.TCP.Tests
{
    [TestFixture]
    public class AuthenticationSpecSslTcp : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();
        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class ConnectionSpecSslTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveSslTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class PrivateClientSpecSslTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class PublishingSpecSslTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class SubscriptionSpecSslTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }
}
