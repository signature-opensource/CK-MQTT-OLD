using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Collections.Generic;
using System.Linq;

namespace CK.MQTT.Sdk.Flows
{
    internal abstract class ProtocolFlowProvider : IProtocolFlowProvider
    {
        protected readonly IMqttTopicEvaluator topicEvaluator;
        protected readonly IRepositoryProvider repositoryProvider;
        protected readonly MqttConfiguration configuration;

        readonly object _lockObject = new object();
        IDictionary<ProtocolFlowType, IProtocolFlow> _flows;

        protected ProtocolFlowProvider( IMqttTopicEvaluator topicEvaluator,
            IRepositoryProvider repositoryProvider,
            MqttConfiguration configuration )
        {
            this.topicEvaluator = topicEvaluator;
            this.repositoryProvider = repositoryProvider;
            this.configuration = configuration;
        }

        protected abstract IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows();

        protected abstract bool IsValidPacketType( MqttPacketType packetType );

        public IProtocolFlow GetFlow( MqttPacketType packetType )
        {
            if( !IsValidPacketType( packetType ) )
            {

                throw new MqttException( ClientProperties.ProtocolFlowProvider_InvalidPacketType( packetType ) );
            }

            ProtocolFlowType flowType = packetType.ToFlowType();


            if( !GetFlows().TryGetValue( flowType, out IProtocolFlow flow ) )
            {
                throw new MqttException( ClientProperties.ProtocolFlowProvider_UnknownPacketType( packetType ) );
            }

            return flow;
        }

        public T GetFlow<T>()
            where T : class, IProtocolFlow
        {
            KeyValuePair<ProtocolFlowType, IProtocolFlow> pair = GetFlows().FirstOrDefault( f => f.Value is T );

            if( pair.Equals( default( KeyValuePair<ProtocolFlowType, IProtocolFlow> ) ) )
            {
                return default;
            }

            return pair.Value as T;
        }

        IDictionary<ProtocolFlowType, IProtocolFlow> GetFlows()
        {
            if( _flows == null )
            {
                lock( _lockObject )
                {
                    if( _flows == null )
                    {
                        _flows = InitializeFlows();
                    }
                }
            }

            return _flows;
        }
    }
}
