using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Tests.Flows
{
    public class PingFlowSpec
    {
        [Test]
        public async Task when_sending_ping_request_then_ping_response_is_sent()
        {
            string clientId = Guid.NewGuid().ToString();
            Mock<IMqttChannel<IPacket>> channel = new Mock<IMqttChannel<IPacket>>();
            IPacket sentPacket = default;

            channel.Setup( c => c.SendAsync( It.IsAny<IPacket>() ) )
                .Callback<IPacket>( packet => sentPacket = packet )
                .Returns( Task.Delay( 0 ) );

            PingFlow flow = new PingFlow();

            await flow.ExecuteAsync( clientId, new PingRequest(), channel.Object )
                .ConfigureAwait( continueOnCapturedContext: false );

            PingResponse pingResponse = sentPacket as PingResponse;

            Assert.NotNull( pingResponse );
            MqttPacketType.PingResponse.Should().Be( pingResponse.Type );
        }
    }
}
