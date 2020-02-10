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
    public class ConnectFormatterSpec
    {
        [Theory]
        [TestCase( "Files/Binaries/Connect_Full.packet", "Files/Packets/Connect_Full.json" )]
        [TestCase( "Files/Binaries/Connect_Min.packet", "Files/Packets/Connect_Min.json" )]
        public async Task when_reading_connect_packet_then_succeeds( string packetPath, string jsonPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            Connect expectedConnect = Packet.ReadPacket<Connect>( jsonPath );
            ConnectFormatter formatter = new ConnectFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedConnect.Should().Be( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Invalid_HeaderFlag.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_ProtocolName.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_ConnectReservedFlag.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_QualityOfService.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_WillFlags.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_UserNamePassword.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_ProtocolLevel.packet" )]
        public void when_reading_invalid_connect_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            ConnectFormatter formatter = new ConnectFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }

        [Theory]
        [TestCase( "Files/Binaries/Connect_Invalid_ClientIdEmptyAndNoCleanSession.packet" )]
        [TestCase( "Files/Binaries/Connect_Invalid_ClientIdBadFormat.packet" )]
        public void when_reading_invalid_client_id_in_connect_packet_then_fails( string packetPath )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            ConnectFormatter formatter = new ConnectFormatter();
            byte[] packet = Packet.ReadAllBytes( packetPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( packet ).Wait() );

            Assert.True( ex.InnerException is MqttConnectionException );
        }

        [Theory]
        [TestCase( "Files/Packets/Connect_Full.json", "Files/Binaries/Connect_Full.packet" )]
        [TestCase( "Files/Packets/Connect_Min.json", "Files/Binaries/Connect_Min.packet" )]
        public async Task when_writing_connect_packet_then_succeeds( string jsonPath, string packetPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            ConnectFormatter formatter = new ConnectFormatter();
            Connect connect = Packet.ReadPacket<Connect>( jsonPath );

            byte[] result = await formatter.FormatAsync( connect )
                .ConfigureAwait( continueOnCapturedContext: false );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        [Theory]
        [TestCase( "Files/Packets/Connect_Invalid_UserNamePassword.json" )]
        [TestCase( "Files/Packets/Connect_Invalid_ClientIdBadFormat.json" )]
        [TestCase( "Files/Packets/Connect_Invalid_ClientIdInvalidLength.json" )]
        public void when_writing_invalid_connect_packet_then_fails( string jsonPath )
        {
            jsonPath = Path.Combine( Environment.CurrentDirectory, jsonPath );

            ConnectFormatter formatter = new ConnectFormatter();
            Connect connect = Packet.ReadPacket<Connect>( jsonPath );

            AggregateException ex = Assert.Throws<AggregateException>( () => formatter.FormatAsync( connect ).Wait() );

            Assert.True( ex.InnerException is MqttException );
        }
    }
}
