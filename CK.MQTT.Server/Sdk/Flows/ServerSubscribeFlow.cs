using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
    internal class ServerSubscribeFlow : IProtocolFlow
    {
        static readonly ITracer _tracer = Tracer.Get<ServerSubscribeFlow>();

        readonly IMqttTopicEvaluator _topicEvaluator;
        readonly IRepository<ClientSession> _sessionRepository;
        readonly IRepository<RetainedMessage> _retainedRepository;
        readonly IPacketIdProvider _packetIdProvider;
        readonly IPublishSenderFlow _senderFlow;
        readonly MqttConfiguration _configuration;

        public ServerSubscribeFlow( IMqttTopicEvaluator topicEvaluator,
            IRepository<ClientSession> sessionRepository,
            IRepository<RetainedMessage> retainedRepository,
            IPacketIdProvider packetIdProvider,
            IPublishSenderFlow senderFlow,
            MqttConfiguration configuration )
        {
            _topicEvaluator = topicEvaluator;
            _sessionRepository = sessionRepository;
            _retainedRepository = retainedRepository;
            _packetIdProvider = packetIdProvider;
            _senderFlow = senderFlow;
            _configuration = configuration;
        }

        public async Task ExecuteAsync( string clientId, IPacket input, IMqttChannel<IPacket> channel )
        {
            if( input.Type != MqttPacketType.Subscribe ) return;

            Subscribe subscribe = input as Subscribe;
            ClientSession session = _sessionRepository.Read( clientId );

            if( session == null )
            {
                throw new MqttException( ServerProperties.SessionRepository_ClientSessionNotFound( clientId ) );
            }

            List<SubscribeReturnCode> returnCodes = new List<SubscribeReturnCode>();

            foreach( Subscription subscription in subscribe.Subscriptions )
            {
                try
                {
                    if( !_topicEvaluator.IsValidTopicFilter( subscription.TopicFilter ) )
                    {
                        _tracer.Error( ServerProperties.ServerSubscribeFlow_InvalidTopicSubscription( subscription.TopicFilter, clientId ) );

                        returnCodes.Add( SubscribeReturnCode.Failure );
                        continue;
                    }

                    ClientSubscription clientSubscription = session
                        .GetSubscriptions()
                        .FirstOrDefault( s => s.TopicFilter == subscription.TopicFilter );

                    if( clientSubscription != null )
                    {
                        clientSubscription.MaximumQualityOfService = subscription.MaximumQualityOfService;
                    }
                    else
                    {
                        clientSubscription = new ClientSubscription
                        {
                            ClientId = clientId,
                            TopicFilter = subscription.TopicFilter,
                            MaximumQualityOfService = subscription.MaximumQualityOfService
                        };

                        session.AddSubscription( clientSubscription );
                    }

                    await SendRetainedMessagesAsync( clientSubscription, channel );

                    MqttQualityOfService supportedQos = _configuration.GetSupportedQos( subscription.MaximumQualityOfService );
                    SubscribeReturnCode returnCode = supportedQos.ToReturnCode();

                    returnCodes.Add( returnCode );
                }
                catch( RepositoryException repoEx )
                {
                    _tracer.Error( repoEx, ServerProperties.ServerSubscribeFlow_ErrorOnSubscription( clientId, subscription.TopicFilter ) );

                    returnCodes.Add( SubscribeReturnCode.Failure );
                }
            }

            _sessionRepository.Update( session );

            await channel.SendAsync( new SubscribeAck( subscribe.PacketId, returnCodes.ToArray() ) );
        }

        async Task SendRetainedMessagesAsync( ClientSubscription subscription, IMqttChannel<IPacket> channel )
        {
            IEnumerable<RetainedMessage> retainedMessages = _retainedRepository
                .ReadAll()
                .Where( r => _topicEvaluator.Matches( topicName: r.Id, topicFilter: subscription.TopicFilter ) );

            if( retainedMessages != null )
            {
                foreach( RetainedMessage retainedMessage in retainedMessages )
                {
                    ushort? packetId = subscription.MaximumQualityOfService == MqttQualityOfService.AtMostOnce ?
                        null : (ushort?)_packetIdProvider.GetPacketId();
                    Publish publish = new Publish( topic: retainedMessage.Id,
                        qualityOfService: subscription.MaximumQualityOfService,
                        retain: true, duplicated: false, packetId: packetId )
                    {
                        Payload = retainedMessage.Payload
                    };

                    await _senderFlow.SendPublishAsync( subscription.ClientId, publish, channel );
                }
            }
        }
    }
}
