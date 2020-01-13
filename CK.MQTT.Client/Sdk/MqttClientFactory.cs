using System.Diagnostics;
using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;
using System;

namespace CK.MQTT.Sdk
{
	/// <summary>
	/// Provides a factory for MQTT Clients
	/// </summary>
	public class MqttClientFactory 
	{
		static readonly ITracer tracer = Tracer.Get<MqttClientFactory> ();

		readonly string connectionString;
		readonly IMqttBinding binding;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientFactory" /> class,
        /// specifying the address to connect and using TCP as the 
        /// default transport protocol binding
        /// </summary>
        /// <param name="hostAddress">Address of the host to connect the client</param>
        public MqttClientFactory (string hostAddress)
            : this (hostAddress, new TcpBinding ())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientFactory" /> class,
        /// specifying the address to connect and the transport protocol binding to use
        /// </summary>
        /// <param name="connectionString">Address of the host to connect the client</param>
        /// <param name="binding">
        /// Transport protocol binding to use as the MQTT underlying protocol
        /// See <see cref="IMqttBinding" /> for more details about how to implement it 
        /// </param>
        public MqttClientFactory (string connectionString, IMqttBinding binding)
        {
            this.connectionString = connectionString;
            this.binding = binding;
        }

        /// <summary>
        /// Creates an MQTT Client
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the Client
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A new MQTT Client</returns>
        /// <exception cref="MqttClientException">MqttClientException</exception>
        public async Task<IMqttClient> CreateClientAsync (MqttConfiguration configuration)
		{
			try {
				//Adding this to not break backwards compatibility related to the method signature
				//Yielding at this point will cause the method to return immediately after it's called,
				//running the rest of the logic acynchronously
				await Task.Yield ();
				
				var topicEvaluator = new MqttTopicEvaluator (configuration);
				var innerChannelFactory = binding.GetChannelFactory (connectionString, configuration);
				var channelFactory = new PacketChannelFactory (innerChannelFactory, topicEvaluator, configuration);
				var packetIdProvider = new PacketIdProvider ();
				var repositoryProvider = new InMemoryRepositoryProvider ();
				var flowProvider = new ClientProtocolFlowProvider (topicEvaluator, repositoryProvider, configuration);

				return new MqttClientImpl (channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration);
			} catch (Exception ex) {
				tracer.Error (ex, Properties.Resources.GetString("Client_InitializeError"));

				throw new MqttClientException  (Properties.Resources.GetString("Client_InitializeError"), ex);
			}
		}
	}
}
