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
    public class SubscribeAckFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet", "Files/Packets/SubscribeAck_SingleTopic.json" )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet", "Files/Packets/SubscribeAck_MultiTopic.json" )]
        public async Task when_reading_subscribe_ack_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            SubscribeAck expectedSubscribeAck = Packet.ReadPacket<SubscribeAck>( jsonPath );
            SubscribeAckFormatter formatter = new SubscribeAckFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedSubscribeAck.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/SubscribeAck_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_subscribe_ack_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            SubscribeAckFormatter formatter = new SubscribeAckFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Binaries/SubscribeAck_Invalid_EmptyReturnCodes.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_Invalid_ReturnCodes.packet" )]
        public void when_reading_invalid_return_code_in_subscribe_ack_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            SubscribeAckFormatter formatter = new SubscribeAckFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Action action = () => formatter.FormatAsync( packet ).Wait();
            action.Should().Throw<MqttException>();
        }

        [Theory]
        [TestCase( "Files/Packets/SubscribeAck_SingleTopic.json", "Files/Binaries/SubscribeAck_SingleTopic.packet" )]
        [TestCase( "Files/Packets/SubscribeAck_MultiTopic.json", "Files/Binaries/SubscribeAck_MultiTopic.packet" )]
        public async Task when_writing_subscribe_ack_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            SubscribeAckFormatter formatter = new SubscribeAckFormatter();
            SubscribeAck subscribeAck = Packet.ReadPacket<SubscribeAck>( jsonPath );

            byte[] result = await formatter.FormatAsync( subscribeAck );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/SubscribeAck_Invalid_EmptyReturnCodes.json" )]
        public void when_writing_invalid_subscribe_ack_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            SubscribeAckFormatter formatter = new SubscribeAckFormatter();
            SubscribeAck subscribeAck = Packet.ReadPacket<SubscribeAck>( jsonPath );

            Assert.Throws<MqttProtocolViolationException>( () => formatter.FormatAsync( subscribeAck ).Wait() );
        }
    }
}
