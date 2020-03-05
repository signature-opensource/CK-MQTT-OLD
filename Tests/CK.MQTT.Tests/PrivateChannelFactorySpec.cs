using CK.Core;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using Moq;
using NUnit.Framework;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace Tests
{
    public class PrivateChannelFactorySpec
    {
        [Test]
        public async Task when_creating_channel_then_succeeds()
        {
            PrivateChannelFactory factory = new PrivateChannelFactory( Mock.Of<ISubject<Mon<PrivateStream>>>(), EndpointIdentifier.Client, new MqttConfiguration() );
            CK.MQTT.Sdk.IMqttChannel<byte[]> channel = await factory.CreateAsync(TestHelper.Monitor);

            Assert.NotNull( channel );
            Assert.True( channel.IsConnected );
        }
    }
}
