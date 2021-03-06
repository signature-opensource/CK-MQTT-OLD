using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using CK.MQTT;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace IntegrationTests.Context
{
	public abstract class IntegrationContext
	{
		static readonly ConcurrentBag<int> usedPorts;
		static readonly Random random = new Random ();

		readonly object lockObject = new object ();
		protected readonly ushort keepAliveSecs;
        protected readonly bool allowWildcardsInTopicFilters;

        static IntegrationContext()
		{
            Tracer.Configuration.AddListener ("CK.MQTT", new TestTracerListener ());
            Tracer.Configuration.SetTracingLevel ("CK.MQTT", SourceLevels.All);

            usedPorts = new ConcurrentBag<int> ();
		}

		public IntegrationContext (ushort keepAliveSecs = 0, bool allowWildcardsInTopicFilters = true)
		{
			this.keepAliveSecs = keepAliveSecs;
            this.allowWildcardsInTopicFilters = allowWildcardsInTopicFilters;
        }

		protected MqttConfiguration Configuration { get; private set; }

		protected async Task<IMqttServer> GetServerAsync (IMqttAuthenticationProvider authenticationProvider = null)
		{
			try {
				LoadConfiguration ();

				var server = MqttServer.Create (Configuration, authenticationProvider: authenticationProvider);

				server.Start ();
				
				return server;
			} catch (MqttException protocolEx) {
				if (protocolEx.InnerException is SocketException) {
					return await GetServerAsync ();
				} else {
					throw;
				}
			}
		}

        protected virtual async Task<IMqttClient> GetClientAsync ()
		{
			LoadConfiguration ();

			return await MqttClient.CreateAsync (IPAddress.Loopback.ToString(), Configuration);
		}

		protected int GetTestLoad()
		{
			var testLoad = 0;
			var loadValue = ConfigurationManager.AppSettings["testLoad"];

			int.TryParse (loadValue, out testLoad);

			return testLoad;
		}

		void LoadConfiguration()
		{
			if (Configuration == null) {
				lock (lockObject) {
					if (Configuration == null) {
						Configuration = new MqttConfiguration {
							BufferSize = 128 * 1024,
							Port = GetPort (),
							KeepAliveSecs = keepAliveSecs,
							WaitTimeoutSecs = 2,
							MaximumQualityOfService = MqttQualityOfService.ExactlyOnce,
							AllowWildcardsInTopicFilters = allowWildcardsInTopicFilters
						};
					}
				}
			}
		}

		static int GetPort()
		{
            return 25565;
        }
	}
}
