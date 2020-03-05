using CK.Core;
using CK.MQTT.Sdk.Bindings;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Storage;
using System;
using System.Diagnostics;
using System.Net;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk
{
    class MqttConnectedClientFactory
    {
        readonly ISubject<Mon<PrivateStream>> _privateStreamListener;

        public MqttConnectedClientFactory( ISubject<Mon<PrivateStream>> privateStreamListener )
        {
            _privateStreamListener = privateStreamListener;
        }

        public async Task<IMqttConnectedClient> CreateClientAsync( IActivityMonitor m, MqttConfiguration configuration )
        {
            try
            {
                //Adding this to not break backwards compatibility related to the method signature
                //Yielding at this point will cause the method to return immediately after it's called,
                //running the rest of the logic acynchronously
                await Task.Yield();

                PrivateBinding binding = new PrivateBinding( _privateStreamListener, EndpointIdentifier.Client );
                MqttTopicEvaluator topicEvaluator = new MqttTopicEvaluator( configuration );
                IMqttChannelFactory innerChannelFactory = binding.GetChannelFactory( IPAddress.Loopback.ToString(), configuration );
                PacketChannelFactory channelFactory = new PacketChannelFactory( innerChannelFactory, topicEvaluator, configuration );
                PacketIdProvider packetIdProvider = new PacketIdProvider();
                InMemoryRepositoryProvider repositoryProvider = new InMemoryRepositoryProvider();
                ClientProtocolFlowProvider flowProvider = new ClientProtocolFlowProvider( topicEvaluator, repositoryProvider, configuration );

                return new MqttConnectedClient( m, channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration );
            }
            catch( Exception ex )
            {
                m.Error( ClientProperties.Client_InitializeError, ex );

                throw new MqttClientException( ClientProperties.Client_InitializeError, ex );
            }
        }
    }

    class MqttConnectedClient : MqttClientImpl, IMqttConnectedClient
    {
        internal MqttConnectedClient( IActivityMonitor m,
            IPacketChannelFactory channelFactory,
            IProtocolFlowProvider flowProvider,
            IRepositoryProvider repositoryProvider,
            IPacketIdProvider packetIdProvider,
            MqttConfiguration configuration )
            : base( m, channelFactory, flowProvider, repositoryProvider, packetIdProvider, configuration )
        {
        }
    }
}
