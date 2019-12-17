using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.Context;
using CK.MQTT;
using NUnit.Framework;

namespace IntegrationTests
{
	public abstract class ConnectionSpecWithKeepAlive : IntegrationContext
    {
        protected ConnectionSpecWithKeepAlive () 
			: base( keepAliveSecs: 1 )
		{
		}

		[Test]
		public async Task when_keep_alive_enabled_and_client_is_disposed_then_server_refresh_active_client_list()
		{
			var client = await GetClientAsync ();

			await client.ConnectAsync (new MqttClientCredentials ( MqttTestHelper.GetClientId ()))
				.ConfigureAwait(continueOnCapturedContext: false);

			var clientId = client.Id;
			var existClientAfterConnect = Server.ActiveClients.Any (c => c == clientId);
			var clientClosed = new ManualResetEventSlim ();

			var subscription = Observable.Create<bool> (observer => {
				var timer = new System.Timers.Timer();

				timer.Interval = 200;
				timer.Elapsed += (sender, args) => {
					if (Server.ActiveClients.Any (c => c == clientId)) {
						observer.OnNext (false);
					} else {
						observer.OnNext (true);
						clientClosed.Set ();
						observer.OnCompleted ();
					}
				};
				timer.Start();

				return () => {
					timer.Dispose();
				};
			})
			.Subscribe (
				_ => { },
				ex => { Console.WriteLine (string.Format ("Error: {0}", ex.Message)); });

			client.Dispose ();

			var serverDetectedClientClosed = clientClosed.Wait (TimeSpan.FromSeconds(KeepAliveSecs * 2));

			subscription.Dispose ();

			Assert.True (existClientAfterConnect);
			Assert.True (serverDetectedClientClosed);
			Assert.False (Server.ActiveClients.Any (c => c == clientId));
		}

		[Test]
		public async Task when_keep_alive_enabled_and_no_packets_are_sent_then_connection_is_maintained()
		{
			var client = await GetClientAsync ();
			var clientId = MqttTestHelper.GetClientId ();

			await client.ConnectAsync (new MqttClientCredentials (clientId))
				.ConfigureAwait(continueOnCapturedContext: false);

            await Task.Delay (TimeSpan.FromSeconds (KeepAliveSecs * 5));

			Assert.True(Server.ActiveClients.Any(c => c == clientId));
			Assert.True(client.IsConnected);
			Assert.False (string.IsNullOrEmpty (client.Id));

			client.Dispose ();
		}
	}
}
