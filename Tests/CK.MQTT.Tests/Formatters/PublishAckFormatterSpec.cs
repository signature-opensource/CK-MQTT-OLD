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
    public class PublishAckFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/PublishAck.packet", "Files/Packets/PublishAck.json" )]
        public async Task when_reading_publish_ack_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            PublishAck expectedPublishAck = Packet.ReadPacket<PublishAck>( jsonPath );
            FlowPacketFormatter<PublishAck> formatter = new FlowPacketFormatter<PublishAck>( MqttPacketType.PublishAck, id => new PublishAck( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPublishAck.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/PublishAck_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_publish_ack_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            FlowPacketFormatter<PublishAck> formatter = new FlowPacketFormatter<PublishAck>( MqttPacketType.PublishAck, id => new PublishAck( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Packets/PublishAck.json", "Files/Binaries/PublishAck.packet" )]
        public async Task when_writing_publish_ack_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            FlowPacketFormatter<PublishAck> formatter = new FlowPacketFormatter<PublishAck>( MqttPacketType.PublishAck, id => new PublishAck( id ) );
            PublishAck publishAck = Packet.ReadPacket<PublishAck>( jsonPath );

            byte[] result = await formatter.FormatAsync( publishAck )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPacket.Should().BeEquivalentTo( result );
        }
    }
}
