using CK.MQTT.Sdk;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tests
{
    public class PacketIdProviderSpec
    {
        [Test]
        public void when_getting_packet_id_then_is_sequencial()
        {
            PacketIdProvider packetIdProvider = new PacketIdProvider();
            int count = 5000;

            for( ushort i = 1; i <= count; i++ )
            {
                packetIdProvider.GetPacketId();
            }

            ushort packetId = packetIdProvider.GetPacketId();

            (count + 1).Should().Be( packetId );
        }

        [Test]
        public void when_getting_packet_id_and_reaches_limit_then_resets()
        {
            PacketIdProvider packetIdProvider = new PacketIdProvider();

            for( int i = 1; i <= ushort.MaxValue; i++ )
            {
                packetIdProvider.GetPacketId();
            }

            ushort packetId = packetIdProvider.GetPacketId();

            1.Should().Be( packetId );
        }

        [Test]
        public void when_getting_packet_id_in_parallel_then_maintains_sequence()
        {
            PacketIdProvider packetIdProvider = new PacketIdProvider();
            int count = 5000;
            List<Task> packetIdTasks = new List<Task>();

            for( ushort i = 1; i <= count; i++ )
            {
                packetIdTasks.Add( Task.Run( () => packetIdProvider.GetPacketId() ) );
            }

            Task.WaitAll( packetIdTasks.ToArray() );

            ushort packetId = packetIdProvider.GetPacketId();

            (count + 1).Should().Be( packetId );
        }
    }
}
