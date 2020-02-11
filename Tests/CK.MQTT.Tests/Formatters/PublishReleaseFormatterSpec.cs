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
    public class PublishReleaseFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/PublishRelease.packet", "Files/Packets/PublishRelease.json" )]
        public async Task when_reading_publish_release_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            PublishRelease expectedPublishRelease = Packet.ReadPacket<PublishRelease>( jsonPath );
            FlowPacketFormatter<PublishRelease> formatter = new FlowPacketFormatter<PublishRelease>( MqttPacketType.PublishRelease, id => new PublishRelease( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedPublishRelease.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/PublishRelease_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_publish_release_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            FlowPacketFormatter<PublishRelease> formatter = new FlowPacketFormatter<PublishRelease>( MqttPacketType.PublishRelease, id => new PublishRelease( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Packets/PublishRelease.json", "Files/Binaries/PublishRelease.packet" )]
        public async Task when_writing_publish_release_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            FlowPacketFormatter<PublishRelease> formatter = new FlowPacketFormatter<PublishRelease>( MqttPacketType.PublishRelease, id => new PublishRelease( id ) );
            PublishRelease publishRelease = Packet.ReadPacket<PublishRelease>( jsonPath );

            byte[] result = await formatter.FormatAsync( publishRelease );

            expectedPacket.Should().BeEquivalentTo( result );
        }
    }
}
