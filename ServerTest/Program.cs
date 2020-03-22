using CK.Core;
using CK.MQTT;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ServerTest
{
    class Program
    {
        static async Task Main()
        {
            var m = new ActivityMonitor();
            var server = MqttServer.Create( m, 5555 );
            server.Start();
            var client = await server.CreateClientAsync( m );
            await client.SubscribeAsync( m, "test", MqttQualityOfService.ExactlyOnce );
            client.MessageReceived += ( m, sender, message ) =>
            {
                if( message.Payload.Length > 0 ) Console.WriteLine( Encoding.UTF8.GetString( message.Payload ) );
            };
            while( true )
            {
                await client.PublishAsync( m, "test", Encoding.UTF8.GetBytes( Console.ReadLine() ) , MqttQualityOfService.ExactlyOnce );
            }
        }
    }
}
