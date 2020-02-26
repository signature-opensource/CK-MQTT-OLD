using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using IntegrationTests.Context;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CK.MQTT.TCP.Tests
{
    static class TcpHelper
    {
        public static IRawTestClient GetRawTestClient( MqttConfiguration config )
        {
            return new TcpRawTestClient( config );
        }

        public static Task<IRawTestClient> ConnectedRawClient( MqttConfiguration config )
        {
            return Task.FromResult<IRawTestClient>( new TcpRawTestClient( config ) );
        }
    }

    [TestFixture]
    public class AuthenticationSpecTcp : AuthenticationSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();
        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }


    [TestFixture]
    public class ConnectionSpecTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class PrivateClientSpecTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class PublishingSpecTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class SubscriptionSpecTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }

    [TestFixture]
    public class BadPacketsTestsTcp : BadPacketsTests
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => new TcpBinding();
        protected override Task<IRawTestClient> GetRawConnectedTestClient() => TcpHelper.ConnectedRawClient( Configuration );
        protected override IRawTestClient GetRawTestClient() => TcpHelper.GetRawTestClient( Configuration );
    }
}
