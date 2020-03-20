using CK.MQTT;
using CK.MQTT.Sdk;
using CK.MQTT.Sdk.Formatters;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Tests.Files.Packets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
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

        static readonly object[] _cases = new object[]
        {
            new object[] { Packets.Publish_Full, "Files/Binaries/Publish_Full.packet" },
            new object[] { Packets.Publish_Min, "Files/Binaries/Publish_Min.packet" }
        };

        [Test, TestCaseSource( nameof( _cases ) )]
        public async Task when_reading_publish_packet_then_succeeds( object publishPacket, string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            Publish expectedPublish = (Publish)publishPacket;
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
        static readonly object[] _cases1 = new object[] {
            new object[] { Packets.Publish_Full , "Files/Binaries/Publish_Full.packet" },
            new object[] { Packets.Publish_Min, "Files/Binaries/Publish_Min.packet" }
        };

        [Test, TestCaseSource( nameof( _cases1 ) )]
        public async Task when_writing_publish_packet_then_succeeds( object publishPacket, string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicName( It.IsAny<string>() ) == true );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            Publish publish = (Publish)publishPacket;

            byte[] result = await formatter.FormatAsync( publish );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        static readonly object[] _cases2 = new object[]
        {
            Packets.Publish_Invalid_Duplicated,
            Packets.Publish_Invalid_Topic,
            Packets.Publish_Invalid_PacketId
        };

        [Test, TestCaseSource( nameof( _cases2 ) )]
        public void when_writing_invalid_publish_packet_then_fails( object packet )
        {
            MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( new MqttConfiguration() );
            PublishFormatter formatter = new PublishFormatter( topicEvaluator );
            Publish publish = (Publish)packet;

            Assert.Throws<MqttException>( () => formatter.FormatAsync( publish ).Wait() );
        }
    }
}
