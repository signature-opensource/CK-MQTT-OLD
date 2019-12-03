using Moq;
using CK.MQTT;
using CK.MQTT.Sdk.Bindings;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests
{
    public class PrivateChannelFactorySpec
    {
        [Test]
        public async Task when_creating_channel_then_succeeds ()
        {
            var factory = new PrivateChannelFactory (Mock.Of<ISubject<PrivateStream>> (), EndpointIdentifier.Client, new MqttConfiguration ());
            var channel = await factory.CreateAsync ();

            Assert.NotNull (channel);
            Assert.True (channel.IsConnected);
        }
    }
}
