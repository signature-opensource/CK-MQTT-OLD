using CK.MQTT;
using FluentAssertions;
using IntegrationTests.Context;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationTests
{
    public class BadPacketsTests : IntegrationContext, IDisposable
    {
        IMqttServer _server;

        [SetUp]
        public void SetUp()
        {
            _server = GetServerAsync().Result;
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Dispose();
        }


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
        public async Task mqtt_stay_alive_after_bad_packet( string packetPath )
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
            using( var correctClient = await GetClientAsync() )
            {
                _server.ClientConnected += Connected;
                var task = correctClient.ConnectAsync();
                Task.WaitAny( Task.Delay( 2000 ), task );
                task.IsCompleted.Should().BeTrue();
                connected.Should().BeTrue();
                _server.ClientConnected -= Connected;
            }
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
