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
    public class UnsubscribeFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet", "Files/Packets/Unsubscribe_SingleTopic.json" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Packets/Unsubscribe_MultiTopic.json" )]
        public async Task when_reading_unsubscribe_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            Unsubscribe expectedUnsubscribe = Packet.ReadPacket<Unsubscribe>( jsonPath );
            UnsubscribeFormatter formatter = new UnsubscribeFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedUnsubscribe.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/Unsubscribe_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_unsubscribe_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            UnsubscribeFormatter formatter = new UnsubscribeFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Unsubscribe_Invalid_EmptyTopics.packet" )]
        public void when_reading_invalid_topic_in_unsubscribe_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            UnsubscribeFormatter formatter = new UnsubscribeFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttProtocolViolationException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Packets/Unsubscribe_SingleTopic.json", "Files/Binaries/Unsubscribe_SingleTopic.packet" )]
        [TestCase( "Files/Packets/Unsubscribe_MultiTopic.json", "Files/Binaries/Unsubscribe_MultiTopic.packet" )]
        public async Task when_writing_unsubscribe_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            UnsubscribeFormatter formatter = new UnsubscribeFormatter();
            Unsubscribe unsubscribe = Packet.ReadPacket<Unsubscribe>( jsonPath );

            byte[] result = await formatter.FormatAsync( unsubscribe );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/Unsubscribe_Invalid_EmptyTopics.json" )]
        public void when_writing_invalid_unsubscribe_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            UnsubscribeFormatter formatter = new UnsubscribeFormatter();
            Unsubscribe unsubscribe = Packet.ReadPacket<Unsubscribe>( jsonPath );

            Assert.Throws<MqttProtocolViolationException>( () => formatter.FormatAsync( unsubscribe ).Wait() );
        }
    }
}
