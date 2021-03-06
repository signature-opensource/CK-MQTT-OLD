using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests
{
    public class PacketProcessorSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet" )]
        [TestCase( "Files/Binaries/Connect_Min.packet" )]
        [TestCase( "Files/Binaries/ConnectAck.packet" )]
        [TestCase( "Files/Binaries/Disconnect.packet" )]
        [TestCase( "Files/Binaries/PingRequest.packet" )]
        [TestCase( "Files/Binaries/PingResponse.packet" )]
        [TestCase( "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Publish_Min.packet" )]
        [TestCase( "Files/Binaries/PublishAck.packet" )]
        [TestCase( "Files/Binaries/PublishComplete.packet" )]
        [TestCase( "Files/Binaries/PublishReceived.packet" )]
        [TestCase( "Files/Binaries/PublishRelease.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet" )]
        public void when_buffering_packet_in_one_sequence_then_get_packet( string packetPath )
        {
            PacketBuffer buffer = new PacketBuffer();

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            bool buffered = buffer.TryGetPackets( readPacket, out IEnumerable<byte[]> bufferedPackets );

            Assert.True( buffered );
            readPacket.Should().BeEquivalentTo( bufferedPackets.First() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet" )]
        [TestCase( "Files/Binaries/Connect_Min.packet" )]
        [TestCase( "Files/Binaries/ConnectAck.packet" )]
        [TestCase( "Files/Binaries/Disconnect.packet" )]
        [TestCase( "Files/Binaries/PingRequest.packet" )]
        [TestCase( "Files/Binaries/PingResponse.packet" )]
        [TestCase( "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Publish_Min.packet" )]
        [TestCase( "Files/Binaries/PublishAck.packet" )]
        [TestCase( "Files/Binaries/PublishComplete.packet" )]
        [TestCase( "Files/Binaries/PublishReceived.packet" )]
        [TestCase( "Files/Binaries/PublishRelease.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet" )]
        public void when_processing_packet_in_multi_sequences_then_get_packet( string packetPath )
        {
            PacketBuffer buffer = new PacketBuffer();

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );
            byte[] sequence1 = readPacket.Bytes( 0, readPacket.Length / 2 );
            byte[] sequence2 = readPacket.Bytes( readPacket.Length / 2, readPacket.Length );

            bool bufferedFirst = buffer.TryGetPackets( sequence1, out IEnumerable<byte[]> bufferedPackets );
            bool bufferedSecond = buffer.TryGetPackets( sequence2, out bufferedPackets );

            Assert.False( bufferedFirst );
            Assert.True( bufferedSecond );
            readPacket.Should().BeEquivalentTo( bufferedPackets.First() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Binaries/PingRequest.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Binaries/Disconnect.packet" )]
        public void when_processing_multi_packets_in_multi_sequences_then_get_packets( string packet1Path, string packet2Path )
        {
            PacketBuffer buffer = new PacketBuffer();

            packet1Path = Path.Combine( Environment.CurrentDirectory, packet1Path );
            packet2Path = Path.Combine( Environment.CurrentDirectory, packet2Path );

            byte[] readPacket1 = Packet.ReadAllBytes( packet1Path );
            byte[] readPacket2 = Packet.ReadAllBytes( packet2Path );

            byte[] sequence1 = new byte[readPacket1.Length + readPacket2.Length / 2];

            Array.Copy( readPacket1, sequence1, readPacket1.Length );
            Array.Copy( readPacket2, 0, sequence1, readPacket1.Length, readPacket2.Length / 2 );

            byte[] sequence2 = readPacket2.Bytes( readPacket2.Length / 2, readPacket2.Length );
            bool bufferedFirst = buffer.TryGetPackets( sequence1, out IEnumerable<byte[]> bufferedPackets1 );
            bool bufferedSecond = buffer.TryGetPackets( sequence2, out IEnumerable<byte[]> bufferedPackets2 );

            Assert.True( bufferedFirst );
            Assert.True( bufferedSecond );
            readPacket1.Should().BeEquivalentTo( bufferedPackets1.First() );
            readPacket2.Should().BeEquivalentTo( bufferedPackets2.First() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Binaries/PingRequest.packet", "Files/Binaries/Subscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet", "Files/Binaries/Publish_Full.packet", "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Publish_Full.packet", "Files/Binaries/Unsubscribe_MultiTopic.packet", "Files/Binaries/Disconnect.packet" )]
        public void when_processing_multi_packets_in_one_sequence_then_get_packets( string packet1Path, string packet2Path, string packet3Path )
        {
            PacketBuffer buffer = new PacketBuffer();

            packet1Path = Path.Combine( Environment.CurrentDirectory, packet1Path );
            packet2Path = Path.Combine( Environment.CurrentDirectory, packet2Path );
            packet3Path = Path.Combine( Environment.CurrentDirectory, packet3Path );

            byte[] readPacket1 = Packet.ReadAllBytes( packet1Path );
            byte[] readPacket2 = Packet.ReadAllBytes( packet2Path );
            byte[] readPacket3 = Packet.ReadAllBytes( packet2Path );

            byte[] sequence = new byte[readPacket1.Length + readPacket2.Length + readPacket3.Length];

            Array.Copy( readPacket1, sequence, readPacket1.Length );
            Array.Copy( readPacket2, 0, sequence, readPacket1.Length, readPacket2.Length );
            Array.Copy( readPacket3, 0, sequence, readPacket1.Length + readPacket2.Length, readPacket3.Length );

            bool bufferedFirst = buffer.TryGetPackets( sequence, out IEnumerable<byte[]> bufferedPackets );

            Assert.True( bufferedPackets.Any() );
            3.Should().Be( bufferedPackets.Count() );
            readPacket1.Should().BeEquivalentTo( bufferedPackets.First() );
            readPacket2.Should().BeEquivalentTo( bufferedPackets.Skip( 1 ).First() );
            readPacket3.Should().BeEquivalentTo( bufferedPackets.Skip( 2 ).First() );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet" )]
        [TestCase( "Files/Binaries/Connect_Min.packet" )]
        [TestCase( "Files/Binaries/ConnectAck.packet" )]
        [TestCase( "Files/Binaries/Disconnect.packet" )]
        [TestCase( "Files/Binaries/PingRequest.packet" )]
        [TestCase( "Files/Binaries/PingResponse.packet" )]
        [TestCase( "Files/Binaries/Publish_Full.packet" )]
        [TestCase( "Files/Binaries/Publish_Min.packet" )]
        [TestCase( "Files/Binaries/PublishAck.packet" )]
        [TestCase( "Files/Binaries/PublishComplete.packet" )]
        [TestCase( "Files/Binaries/PublishReceived.packet" )]
        [TestCase( "Files/Binaries/PublishRelease.packet" )]
        [TestCase( "Files/Binaries/Subscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Subscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/SubscribeAck_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_MultiTopic.packet" )]
        [TestCase( "Files/Binaries/Unsubscribe_SingleTopic.packet" )]
        [TestCase( "Files/Binaries/UnsubscribeAck.packet" )]
        public void when_processing_incomplete_packet_then_does_not_get_packet( string packetPath )
        {
            PacketBuffer buffer = new PacketBuffer();

            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] readPacket = Packet.ReadAllBytes( packetPath );

            readPacket = readPacket.Bytes( 0, readPacket.Length - 2 );

            bool buffered = buffer.TryGetPackets( readPacket, out IEnumerable<byte[]> bufferedPackets );

            Assert.False( buffered );
            Assert.False( bufferedPackets.Any() );
        }
    }
}
