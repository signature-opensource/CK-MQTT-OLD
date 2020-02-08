using System;
using System.Threading.Tasks;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using Moq;
using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

namespace Tests.Flows
{
	public class PingFlowSpec
	{
		[Test]
		public async Task when_sending_ping_request_then_ping_response_is_sent()
		{
			var clientId = Guid.NewGuid ().ToString ();
			var channel = new Mock<IMqttChannel<IPacket>> ();
			var sentPacket = default(IPacket);

			channel.Setup (c => c.SendAsync(TestHelper.Monitor, It.IsAny<IPacket>()))
				.Callback<IPacket> (packet => sentPacket = packet)
				.Returns(Task.Delay(0));

			var flow = new PingFlow ();

			await flow.ExecuteAsync (clientId, new PingRequest(), channel.Object)
				.ConfigureAwait(continueOnCapturedContext: false);

			var pingResponse = sentPacket as PingResponse;

			Assert.NotNull (pingResponse);
			MqttPacketType.PingResponse.Should().Be(pingResponse.Type);
		}
	}
}
