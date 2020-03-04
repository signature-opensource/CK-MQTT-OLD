using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Tests.Formatters
{
    public class PublishFormatterSpec
    {
        readonly Mock<IMqttChannel<IPacket>> packetChannel;
        readonly Mock<IMqttChannel<byte[]>> byteChannel;

        public PublishFormatterSpec()
        {
            packetChannel = new Mock<IMqttChannel<IPacket>>();
            byteChannel = new Mock<IMqttChannel<byte[]>>();
        }

        [Theory]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Packets/Publish_Full.json" )]
        [TestCase( "Files/Binaries/Publish_Min.packet", "Files/Packets/Publish_Min.json" )]
        public async Task when_reading_publish_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            Publish expectedPublish = Packet.ReadPacket<Publish>( jsonPath );
            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicName( It.IsAny<string>() ) == true );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedPublish.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/Publish_Invalid_QualityOfService.packet" )]
        [TestCase( "Files/Binaries/Publish_Invalid_Duplicated.packet" )]
        [TestCase( "Files/Binaries/Publish_Invalid_Topic.packet" )]
        public void when_reading_invalid_publish_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Packets/Publish_Full.json", "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Packets/Publish_Min.json", "Files/Binaries/Publish_Min.packet" )]
        public async Task when_writing_publish_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicName( It.IsAny<string>() ) == true );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            Publish publish = Packet.ReadPacket<Publish>( jsonPath );

            byte[] result = await formatter.FormatAsync( publish );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/Publish_Invalid_Duplicated.json" )]
        [TestCase( "Files/Packets/Publish_Invalid_Topic.json" )]
        [TestCase( "Files/Packets/Publish_Invalid_PacketId.json" )]
        public void when_writing_invalid_publish_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            Publish publish = Packet.ReadPacket<Publish>( jsonPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( publish ).Wait() );
        }
    }
}
