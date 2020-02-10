using CK.MQTT;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tests.Formatters
{
    public class PublishReceivedFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/PublishReceived.packet", "Files/Packets/PublishReceived.json" )]
        public async Task when_reading_publish_received_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            PublishReceived expectedPublishReceived = Packet.ReadPacket<PublishReceived>( jsonPath );
            FlowPacketFormatter<PublishReceived> formatter = new FlowPacketFormatter<PublishReceived>( MqttPacketType.PublishReceived, id => new PublishReceived( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPublishReceived.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/PublishReceived_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_publish_received_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            FlowPacketFormatter<PublishReceived> formatter = new FlowPacketFormatter<PublishReceived>( MqttPacketType.PublishReceived, id => new PublishReceived( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Packets/PublishReceived.json", "Files/Binaries/PublishReceived.packet" )]
        public async Task when_writing_publish_received_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            FlowPacketFormatter<PublishReceived> formatter = new FlowPacketFormatter<PublishReceived>( MqttPacketType.PublishReceived, id => new PublishReceived( id ) );
            PublishReceived publishReceived = Packet.ReadPacket<PublishReceived>( jsonPath );

            byte[] result = await formatter.FormatAsync( publishReceived )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPacket.Should().BeEquivalentTo( result );
        }
    }
}
