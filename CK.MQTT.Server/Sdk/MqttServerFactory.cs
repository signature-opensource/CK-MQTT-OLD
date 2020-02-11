using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Storage;
using System;
using System.Diagnostics;
using System.Reactive.Subjects;

namespace CK.MQTT.Sdk
{
    /// <summary>
    /// Provides a factory for MQTT Servers
    /// </summary>
    public class MqttServerFactory
    {
        static readonly ITracer _tracer = Tracer.Get<MqttServerFactory>();

        readonly IMqttServerBinding _binding;
        readonly IMqttAuthenticationProvider _authenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerFactory" /> class,
        /// with the option to define an authentication provider to enable authentication
        /// as part of the connection mechanism.
        /// It also uses TCP as the default transport protocol binding
        /// </summary>
        /// <param name="authenticationProvider">
        /// Optional authentication provider to use, 
        /// to enable authentication as part of the connection mechanism
        /// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
        /// an authentication provider
        /// </param>
        public MqttServerFactory( IMqttAuthenticationProvider authenticationProvider = null )
            : this( new ServerTcpBinding(), authenticationProvider )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttServerFactory" /> class,
        /// spcifying the transport protocol binding to use and optionally the 
        /// authentication provider to enable authentication as part of the connection mechanism.
        /// </summary>
        /// <param name="binding">
        /// Transport protocol binding to use as the MQTT underlying protocol
        /// See <see cref="IMqttServerBinding" /> for more details about how to implement it 
        /// </param>
        /// <param name="authenticationProvider">
        /// Optional authentication provider to use, 
        /// to enable authentication as part of the connection mechanism
        /// See <see cref="IMqttAuthenticationProvider" /> for more details about how to implement
        /// an authentication provider
        /// </param>
        public MqttServerFactory( IMqttServerBinding binding, IMqttAuthenticationProvider authenticationProvider = null )
        {
            _binding = binding;
            _authenticationProvider = authenticationProvider ?? NullAuthenticationProvider.Instance;
        }

        /// <summary>
        /// Creates an MQTT Server
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for creating the Server
        /// See <see cref="MqttConfiguration" /> for more details about the supported values
        /// </param>
        /// <returns>A new MQTT Server</returns>
        /// <exception cref="MqttServerException">MqttServerException</exception>
        public IMqttServer CreateServer( MqttConfiguration configuration )
        {
            try
            {
                MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( configuration );
                IMqttChannelListener channelProvider = _binding.GetChannelListener( configuration );
                PacketChannelFactory channelFactory = new PacketChannelFactory( topicEvaluator, configuration );
                InMemoryRepositoryProvider repositoryProvider = new InMemoryRepositoryProvider();
                ConnectionProvider connectionProvider = new ConnectionProvider();
                PacketIdProvider packetIdProvider = new PacketIdProvider();
                Subject<MqttUndeliveredMessage> undeliveredMessagesListener = new Subject<MqttUndeliveredMessage>();
                ServerProtocolFlowProvider flowProvider = new ServerProtocolFlowProvider( _authenticationProvider, connectionProvider, topicEvaluator,
                    repositoryProvider, packetIdProvider, undeliveredMessagesListener, configuration );

                return new MqttServerImpl( channelProvider, channelFactory,
                    flowProvider, connectionProvider, undeliveredMessagesListener, configuration );
            }
            catch( Exception ex )
            {
                _tracer.Error( ex, ServerProperties.Server_InitializeError );

                throw new MqttServerException( ServerProperties.Server_InitializeError, ex );
            }
        }
    }
}
