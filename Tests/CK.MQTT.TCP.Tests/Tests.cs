using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using NUnit.Framework;

namespace CK.MQTT.TCP.Tests
{
    [TestFixture]
    public class AuthenticationSpecTcp : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );
        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class ConnectionSpecTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class PrivateClientSpecTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class PublishingSpecTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class SubscriptionSpecTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }

    [TestFixture]
    public class KeepAliveSpecTcp : KeepAliveSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding( 25565 );

        protected override IMqttBinding MqttBinding => new TcpBinding();
    }
}
