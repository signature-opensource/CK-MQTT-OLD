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
    public class EmptyPacketFormatterSpec
    {
        readonly Mock<IMqttChannel<IPacket>> packetChannel;
        readonly Mock<IMqttChannel<byte[]>> byteChannel;

        public EmptyPacketFormatterSpec()
        {
            packetChannel = new Mock<IMqttChannel<IPacket>>();
            byteChannel = new Mock<IMqttChannel<byte[]>>();
        }

        [Theory]
        [TestCase( "Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof( PingResponse ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof( Disconnect ) )]
        public async Task when_reading_empty_packet_then_succeeds( string packetPath, MqttPacketType packetType, Type type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            IFormatter formatter = GetFormatter( packetType, type );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            IPacket result = await formatter.FormatAsync( packet );

            Assert.NotNull( result );
        }

        [Theory]
        [TestCase( "Files/Binaries/PingResponse_Invalid_HeaderFlag.packet", MqttPacketType.PingResponse, typeof( PingResponse ) )]
        [TestCase( "Files/Binaries/PingRequest_Invalid_HeaderFlag.packet", MqttPacketType.PingRequest, typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/Disconnect_Invalid_HeaderFlag.packet", MqttPacketType.Disconnect, typeof( Disconnect ) )]
        public void when_reading_invalid_empty_packet_then_fails( string packetPath, MqttPacketType packetType, Type type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            IFormatter formatter = GetFormatter( packetType, type );
            byte[] packet = Packet.ReadAllBytes( packetPath );

            Assert.Throws<MqttException>( () => formatter.FormatAsync( packet ).Wait() );
        }

        [Theory]
        [TestCase( "Files/Binaries/PingResponse.packet", MqttPacketType.PingResponse, typeof( PingResponse ) )]
        [TestCase( "Files/Binaries/PingRequest.packet", MqttPacketType.PingRequest, typeof( PingRequest ) )]
        [TestCase( "Files/Binaries/Disconnect.packet", MqttPacketType.Disconnect, typeof( Disconnect ) )]
        public async Task when_writing_empty_packet_then_succeeds( string packetPath, MqttPacketType packetType, Type type )
        {
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );

            byte[] expectedPacket = Packet.ReadAllBytes( packetPath );
            IFormatter formatter = GetFormatter( packetType, type );
            IPacket packet = Activator.CreateInstance( type ) as IPacket;

            byte[] result = await formatter.FormatAsync( packet );

            expectedPacket.Should().BeEquivalentTo( result );
        }

        IFormatter GetFormatter( MqttPacketType packetType, Type type )
        {
            Type genericType = typeof( EmptyPacketFormatter<> );
            Type formatterType = genericType.MakeGenericType( type );
            IFormatter formatter = Activator.CreateInstance( formatterType, packetType ) as IFormatter;

            return formatter;
        }
    }
}
