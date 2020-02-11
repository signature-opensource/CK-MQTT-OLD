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
    public class ConnectAckFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/ConnectAck.packet", "Files/Packets/ConnectAck.json" )]
        public async Task when_reading_connect_ack_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            ConnectAck expectedConnectAck = Packet.ReadPacket<ConnectAck>( jsonPath );
            ConnectAckFormatter formatter = new ConnectAckFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            expectedConnectAck.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/ConnectAck_Invalid_HeaderFlag.packet" )]
        [TestCase( "Files/Binaries/ConnectAck_Invalid_AckFlags.packet" )]
        [TestCase( "Files/Binaries/ConnectAck_Invalid_SessionPresent.packet" )]
        public void when_reading_invalid_connect_ack_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            ConnectAckFormatter formatter = new ConnectAckFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Packets/ConnectAck.json", "Files/Binaries/ConnectAck.packet" )]
        public async Task when_writing_connect_ack_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            ConnectAckFormatter formatter = new ConnectAckFormatter();
            ConnectAck connectAck = Packet.ReadPacket<ConnectAck>( jsonPath );

            byte[] result = await formatter.FormatAsync( connectAck );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/ConnectAck_Invalid_SessionPresent.json" )]
        public void when_writing_invalid_connect_ack_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            ConnectAckFormatter formatter = new ConnectAckFormatter();
            ConnectAck connectAck = Packet.ReadPacket<ConnectAck>( jsonPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( connectAck ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }
    }
}
