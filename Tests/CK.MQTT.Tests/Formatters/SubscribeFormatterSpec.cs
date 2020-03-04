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
    public class SubscribeFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet", "Files/Packets/Subscribe_SingleTopic.json" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Packets/Subscribe_MultiTopic.json" )]
        public async Task when_reading_subscribe_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            Subscribe expectedSubscribe = Packet.ReadPacket<Subscribe>( jsonPath );
            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicFilter( It.IsAny<string>() ) == true );
            SubscribeFormatter formatter = new SubscribeFormatter( topicEvaluator );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedSubscribe.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/Subscribe_Invalid_HeaderFlag.packet" )]
        public void when_reading_invalid_subscribe_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicFilter( It.IsAny<string>() ) == true );
            SubscribeFormatter formatter = new SubscribeFormatter( topicEvaluator );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Subscribe_Invalid_TopicFilterQosPair.packet" )]
        [TestCase( "Files/Binaries/Subscribe_Invalid_TopicFilterQosPair2.packet" )]
        [TestCase( "Files/Binaries/Subscribe_Invalid_TopicFilterQos.packet" )]
        public void when_reading_invalid_topic_filter_in_subscribe_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicFilter( It.IsAny<string>() ) == true );
            SubscribeFormatter formatter = new SubscribeFormatter( topicEvaluator );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttProtocolViolationException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Packets/Subscribe_SingleTopic.json", "Files/Binaries/Subscribe_SingleTopic.packet" )]
        [TestCase( "Files/Packets/Subscribe_MultiTopic.json", "Files/Binaries/Subscribe_MultiTopic.packet" )]
        public async Task when_writing_subscribe_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicFilter( It.IsAny<string>() ) == true );
            SubscribeFormatter formatter = new SubscribeFormatter( topicEvaluator );
            Subscribe subscribe = Packet.ReadPacket<Subscribe>( jsonPath );

            byte[] result = await formatter.FormatAsync( subscribe );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/Subscribe_Invalid_EmptyTopicFilters.json" )]
        public void when_writing_invalid_subscribe_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            IMqttTopicEvaluator topicEvaluator = Mock.Of<IMqttTopicEvaluator>( e => e.IsValidTopicFilter( It.IsAny<string>() ) == true );
            SubscribeFormatter formatter = new SubscribeFormatter( topicEvaluator );
            Subscribe subscribe = Packet.ReadPacket<Subscribe>( jsonPath );

            Assert.Throws<MqttProtocolViolationException>( () => formatter.FormatAsync( subscribe ).Wait() );
        }
    }
}
