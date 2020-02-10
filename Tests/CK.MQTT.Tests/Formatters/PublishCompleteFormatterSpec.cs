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
    public class PublishCompleteFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/PublishComplete.packet", "Files/Packets/PublishComplete.json" )]
        public async Task when_reading_publish_complete_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            PublishComplete expectedPublishComplete = Packet.ReadPacket<PublishComplete>( jsonPath );
            FlowPacketFormatter<PublishComplete> formatter = new FlowPacketFormatter<PublishComplete>( MqttPacketType.PublishComplete, id => new PublishComplete( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPublishComplete.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/PublishComplete_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_publish_complete_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            FlowPacketFormatter<PublishComplete> formatter = new FlowPacketFormatter<PublishComplete>( MqttPacketType.PublishComplete, id => new PublishComplete( id ) );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Packets/PublishComplete.json", "Files/Binaries/PublishComplete.packet" )]
        public async Task when_writing_publish_complete_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            FlowPacketFormatter<PublishComplete> formatter = new FlowPacketFormatter<PublishComplete>( MqttPacketType.PublishComplete, id => new PublishComplete( id ) );
            PublishComplete publishComplete = Packet.ReadPacket<PublishComplete>( jsonPath );

            byte[] result = await formatter.FormatAsync( publishComplete )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPacket.Should().BeEquivalentTo( result );
        }
    }
}
