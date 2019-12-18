using CK.Core;
using CK.MQTT.Proxy.FakeClient;
using CK.MQTT.Sdk.Bindings;
using IntegrationTests;
using NUnit.Framework;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
namespace CK.MQTT.Stub.Tests
{

    public static class TestStubHelper
    {
        public static IMqttClient MqttClientInstance;
        public static MqttRelay Relay;
        public static async Task<IMqttClient> GetMqttClient()
        {
            if( MqttClientInstance == null )
            {
                MqttClientInstance = await MqttClient.CreateAsync( "localhost", 25565 );
                Relay = new MqttRelay( TestHelper.Monitor, MqttClientInstance );
                await Relay.StartAsync( CancellationToken.None );
            }
            IActivityMonitor m = new ActivityMonitor();
            return MqttStubClient.Create( m, 5 );
        }
    }

    [TestFixture]
    public class ConnectionSpecTcp : ConnectionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => null;
        protected override Task<IMqttClient> GetClientAsync()
        {
            return TestStubHelper.GetMqttClient();
        }
    }

    [TestFixture]
    public class ConnectionSpecWithKeepAliveTcp : ConnectionSpecWithKeepAlive
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => null;
        protected override Task<IMqttClient> GetClientAsync()
        {
            return TestStubHelper.GetMqttClient();
        }
    }

    [TestFixture]
    public class PrivateClientSpecTcp : PrivateClientSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => null;
        protected override Task<IMqttClient> GetClientAsync()
        {
            return TestStubHelper.GetMqttClient();
        }
    }

    [TestFixture]
    public class PublishingSpecTcp : PublishingSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => null;
        protected override Task<IMqttClient> GetClientAsync()
        {
            return TestStubHelper.GetMqttClient();
        }
    }

    [TestFixture]
    public class SubscriptionSpecTcp : SubscriptionSpec
    {
        protected override IMqttServerBinding MqttServerBinding => new ServerTcpBinding();

        protected override IMqttBinding MqttBinding => null;
        protected override Task<IMqttClient> GetClientAsync()
        {
            return TestStubHelper.GetMqttClient();
        }
    }
}
