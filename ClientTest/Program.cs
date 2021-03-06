using CK.Core;
using CK.MQTT;
using CK.MQTT.Ssl;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ClientTest
{
    class Program
    {
        static async Task Main( string[] args )
        {
            var m = new ActivityMonitor();
            var client = await MqttClient.CreateAsync( m, "127.0.0.1", new MqttConfiguration() { Port = 5555 } );
            Console.WriteLine( "Press any key to connect" );
            Console.ReadKey();
            await client.ConnectAsync( m, new MqttClientCredentials( "testclient" ) );
            client.MessageStream.Subscribe( ( message ) =>
            {
                if( message.Item.Payload?.Length > 0 ) Console.WriteLine( Encoding.UTF8.GetString( message.Item.Payload ) );
            } );
            while( true )
            {
                await client.PublishAsync( m,
                    new MqttApplicationMessage( "test/hist_norm_cumul", Encoding.UTF8.GetBytes( Console.ReadLine() ) )
                    , MqttQualityOfService.ExactlyOnce );
            }
        }
    }
}
