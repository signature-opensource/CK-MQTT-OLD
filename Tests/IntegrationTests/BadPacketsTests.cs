using CK.Core;
using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using static CK.Testing.MonitorTestHelper;

namespace IntegrationTests
{
    public abstract class BadPacketsTests : IntegrationContext, IDisposable
    {
        [TestCase( "Files/RandomPacketIFoundOnMyPc.bin" )]
        public async Task mqtt_stay_alive_after_bad_packet_instead_of_ssl_handshake( string packetPath )
        {
            await AssertServerShouldBeRunning();
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            using( var client = GetRawTestClient() )
            {
                await client.SendRawBytesAsync( await File.ReadAllBytesAsync( packetPath ) );
            }

            await AssertServerShouldBeRunning();
        }

        [TestCase( "Files/RandomPacketIFoundOnMyPc.bin" )]
        public async Task mqtt_stay_alive_after_channel_etablished( string packetPath )
        {
            await AssertServerShouldBeRunning();
            packetPath = Path.Combine( Environment.CurrentDirectory, packetPath );
            using( var client = await GetRawConnectedTestClient() )
            {
                await client.SendRawBytesAsync( await File.ReadAllBytesAsync( packetPath ) );
            }

            await AssertServerShouldBeRunning();
        }

        async Task AssertServerShouldBeRunning()
        {
            await AsserServerRunning();
            await AsserServerRunning();
            await AsserServerRunning();
        }

        async Task AsserServerRunning()
        {
            bool connected = false;
            void Connected( object sender, string clientId )
            {
                connected = true;
            }
            (IMqttClient clientThatWork, IActivityMonitor m) = await GetClientAsync( "Client testing that server is alive." );
            Server.ClientConnected += Connected;
            var task = clientThatWork.ConnectAnonymousAsync( m );
            Task.WaitAny( Task.Delay( 2000 ), task );
            task.IsCompleted.Should().BeTrue();
            await Task.Delay( 50 );
            connected.Should().BeTrue();
            Server.ClientConnected -= Connected;
            await clientThatWork.DisconnectAsync( m );
        }

    }
}
