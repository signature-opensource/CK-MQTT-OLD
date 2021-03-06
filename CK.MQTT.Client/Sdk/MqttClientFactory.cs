using CK.Core;
using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Storage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    /// <summary>
    /// Provides a factory for MQTT Clients
    /// </summary>
    public class MqttClientFactory
    {
        readonly string _hostAddress;
        readonly IMqttBinding _binding;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientFactory" /> class,
        /// specifying the address to connect and using TCP as the 
        /// default transport protocol binding
        /// </summary>
        /// <param name="hostAddress">Address of the host to connect the client</param>
        public MqttClientFactory( string hostAddress )
            : this( hostAddress, new TcpBinding() )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttClientFactory" /> class,
        /// specifying the address to connect and the transport protocol binding to use
        /// </summary>
        /// <param name="hostAddress">Address of the host to connect the client</param>
        /// <param name="binding">
        /// Transport protocol binding to use as the MQTT underlying protocol
        /// See <see cref="IMqttBinding" /> for more details about how to implement it 
        /// </param>
        public MqttClientFactory( string hostAddress, IMqttBinding binding )
        {
            _hostAddress = hostAddress;
            _binding = binding;
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
        public async Task<IMqttClient> CreateClientAsync( IActivityMonitor m, MqttConfiguration configuration )//TODO: make it synchronous.
        {
            try
            {
                //Adding this to not break backwards compatibility related to the method signature
                //Yielding at this point will cause the method to return immediately after it's called,
                //running the rest of the logic acynchronously
                await Task.Yield();

                MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( configuration );
                IMqttChannelFactory innerChannelFactory = _binding.GetChannelFactory( _hostAddress, configuration );
                PacketChannelFactory channelFactory = new PacketChannelFactory( innerChannelFactory, topicEvaluator, configuration );
                PacketIdProvider packetIdProvider = new PacketIdProvider();
                InMemoryRepositoryProvider repositoryProvider = new InMemoryRepositoryProvider();
                ClientProtocolFlowProvider flowProvider = new ClientProtocolFlowProvider( topicEvaluator, repositoryProvider, configuration );

                return new MqttClientImpl( m, channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration );
            }
            catch( Exception ex )
            {
                m.Error( ClientProperties.Client_InitializeError, ex );

                throw new MqttClientException( ClientProperties.Client_InitializeError, ex );
            }
        }
    }
}
