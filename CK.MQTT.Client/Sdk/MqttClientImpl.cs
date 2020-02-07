using System.Diagnostics;
using System.Linq;
using CK.MQTT.Sdk.Flows;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System;

namespace CK.MQTT.Sdk
{
	internal class MqttClientImpl : IMqttClient
	{
		static readonly ITracer _tracer = Tracer.Get<MqttClientImpl>();

		bool _disposed;
		bool _isProtocolConnected;
		IPacketListener _packetListener;
		IDisposable _packetsSubscription;
		Subject<MqttApplicationMessage> _receiver;

		readonly IPacketChannelFactory _channelFactory;
		readonly IProtocolFlowProvider _flowProvider;
		readonly IRepository<ClientSession> _sessionRepository;
		readonly IPacketIdProvider _packetIdProvider;
		readonly MqttConfiguration _configuration;

		internal MqttClientImpl(IPacketChannelFactory channelFactory,
			IProtocolFlowProvider flowProvider,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			MqttConfiguration configuration)
		{
			_receiver = new Subject<MqttApplicationMessage>();
			_channelFactory = channelFactory;
			_flowProvider = flowProvider;
			_sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			_packetIdProvider = packetIdProvider;
			_configuration = configuration;
		}

		public event EventHandler<MqttEndpointDisconnected> Disconnected = (sender, args) => { };

		public string Id { get; private set; }

		public bool IsConnected
		{
			get
			{
				CheckUnderlyingConnection();
				return _isProtocolConnected && Channel.IsConnected;
			}
			private set => _isProtocolConnected = value;
        }

		public IObservable<MqttApplicationMessage> MessageStream => _receiver;

        internal IMqttChannel<IPacket> Channel { get; private set; }

		public async Task<SessionState> ConnectAsync(MqttClientCredentials credentials, MqttLastWill will = null, bool cleanSession = false)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				if (IsConnected)
				{
					throw new MqttClientException(string.Format(Properties.Client_AlreadyConnected, Id));
				}

				if (string.IsNullOrEmpty(credentials.ClientId) && !cleanSession)
				{
					throw new MqttClientException(Properties.Client_AnonymousClientWithoutCleanSession);
				}

				Id = string.IsNullOrEmpty(credentials.ClientId) ?
					MqttClient.GetAnonymousClientId() :
					credentials.ClientId;

				OpenClientSession(cleanSession);

				await InitializeChannelAsync().ConfigureAwait(continueOnCapturedContext: false);

				var connect = new Connect(Id, cleanSession)
				{
					UserName = credentials.UserName,
					Password = credentials.Password,
					Will = will,
					KeepAlive = _configuration.KeepAliveSecs
				};

				await SendPacketAsync(connect)
					.ConfigureAwait(continueOnCapturedContext: false);

				var connectTimeout = TimeSpan.FromSeconds(_configuration.WaitTimeoutSecs);
				var ack = await _packetListener
					.PacketStream
					.ObserveOn(NewThreadScheduler.Default)
					.OfType<ConnectAck>()
					.FirstOrDefaultAsync()
					.Timeout(connectTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Client_ConnectionDisconnected, Id);

					throw new MqttClientException(message);
				}

				if (ack.Status != MqttConnectionStatus.Accepted)
				{
					throw new MqttConnectionException(ack.Status);
				}

				IsConnected = true;

				return ack.SessionPresent ? SessionState.SessionPresent : SessionState.CleanSession;
			}
			catch (TimeoutException timeEx)
			{
				Close(timeEx);
				throw new MqttClientException(string.Format(Properties.Client_ConnectionTimeout, Id), timeEx);
			}
			catch (MqttConnectionException connectionEx)
			{
				Close(connectionEx);

				var message = string.Format(Properties.Client_ConnectNotAccepted, Id, connectionEx.ReturnCode);

				throw new MqttClientException(message, connectionEx);
			}
			catch (MqttClientException clientEx)
			{
				Close(clientEx);
				throw;
			}
			catch (Exception ex)
			{
				Close(ex);
				throw new MqttClientException(string.Format(Properties.Client_ConnectionError, Id), ex);
			}
		}

		public Task<SessionState> ConnectAsync(MqttLastWill will = null) =>
			ConnectAsync(new MqttClientCredentials(), will, cleanSession: true);

		public async Task SubscribeAsync(string topicFilter, MqttQualityOfService qos)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				var packetId = _packetIdProvider.GetPacketId();
				var subscribe = new Subscribe(packetId, new Subscription(topicFilter, qos));

                var subscribeTimeout = TimeSpan.FromSeconds(_configuration.WaitTimeoutSecs);

				await SendPacketAsync(subscribe)
					.ConfigureAwait(continueOnCapturedContext: false);

				var ack = await _packetListener
                    .PacketStream
                    .ObserveOn(NewThreadScheduler.Default)
                    .OfType<SubscribeAck>()
                    .FirstOrDefaultAsync(x => x.PacketId == packetId)
                    .Timeout(subscribeTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Client_SubscriptionDisconnected, Id, topicFilter);

					_tracer.Error(message);

					throw new MqttClientException(message);
				}

				if (ack.ReturnCodes.FirstOrDefault() == SubscribeReturnCode.Failure)
				{
					var message = string.Format(Properties.Client_SubscriptionRejected, Id, topicFilter);

					_tracer.Error(message);

					throw new MqttClientException(message);
				}
			}
			catch (TimeoutException timeEx)
			{
				Close(timeEx);

				var message = string.Format(Properties.Client_SubscribeTimeout, Id, topicFilter);

				throw new MqttClientException(message, timeEx);
			}
			catch (MqttClientException clientEx)
			{
				Close(clientEx);
				throw;
			}
			catch (Exception ex)
			{
				Close(ex);

				var message = string.Format(Properties.Client_SubscribeError, Id, topicFilter);

				throw new MqttClientException(message, ex);
			}
		}

		public async Task PublishAsync(MqttApplicationMessage message, MqttQualityOfService qos, bool retain = false)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				ushort? packetId = qos == MqttQualityOfService.AtMostOnce ? null : (ushort?)_packetIdProvider.GetPacketId();
				var publish = new Publish(message.Topic, qos, retain, duplicated: false, packetId: packetId)
				{
					Payload = message.Payload
				};

				var senderFlow = _flowProvider.GetFlow<PublishSenderFlow>();

				await Task.Run(async () =>
				{
					await senderFlow.SendPublishAsync(Id, publish, Channel)
						.ConfigureAwait(continueOnCapturedContext: false);
				}).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception ex)
			{
				Close(ex);
				throw;
			}
		}

		public async Task UnsubscribeAsync(params string[] topics)
		{
			if (_disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			try
			{
				topics = topics ?? Array.Empty<string>();

				var packetId = _packetIdProvider.GetPacketId();
				var unsubscribe = new Unsubscribe(packetId, topics);

                var unsubscribeTimeout = TimeSpan.FromSeconds(_configuration.WaitTimeoutSecs);

				await SendPacketAsync(unsubscribe)
					.ConfigureAwait(continueOnCapturedContext: false);

				var ack = await _packetListener
                    .PacketStream
                    .ObserveOn(NewThreadScheduler.Default)
                    .OfType<UnsubscribeAck>()
                    .FirstOrDefaultAsync(x => x.PacketId == packetId)
                    .Timeout(unsubscribeTimeout);

				if (ack == null)
				{
					var message = string.Format(Properties.Client_UnsubscribeDisconnected, Id, string.Join(", ", topics));

					_tracer.Error(message);

					throw new MqttClientException(message);
				}
			}
			catch (TimeoutException timeEx)
			{
				Close(timeEx);

				var message = string.Format(Properties.Client_UnsubscribeTimeout, Id, string.Join(", ", topics));

				_tracer.Error(message);

				throw new MqttClientException(message, timeEx);
			}
			catch (MqttClientException clientEx)
			{
				Close(clientEx);
				throw;
			}
			catch (Exception ex)
			{
				Close(ex);

				var message = string.Format(Properties.Client_UnsubscribeError, Id, string.Join(", ", topics));

				_tracer.Error(message);

				throw new MqttClientException(message, ex);
			}
		}

		public async Task DisconnectAsync()
		{
			try
			{
				if (!IsConnected)
				{
					throw new MqttClientException(Properties.Client_AlreadyDisconnected);
				}

				_packetsSubscription?.Dispose();

				await SendPacketAsync(new Disconnect())
					.ConfigureAwait(continueOnCapturedContext: false);

				await _packetListener
					.PacketStream
					.LastOrDefaultAsync();

				Close(DisconnectedReason.SelfDisconnected);
			}
			catch (Exception ex)
			{
				Close(ex);
			}
		}

		void IDisposable.Dispose()
		{
			DisposeAsync(disposing: true).Wait();
			GC.SuppressFinalize(this);
		}

		protected virtual async Task DisposeAsync(bool disposing)
		{
			if (_disposed) return;

			if (disposing)
			{
				if (IsConnected)
				{
					await DisconnectAsync().ConfigureAwait(continueOnCapturedContext: false);
				}

				_disposed = true;
			}
		}

		void Close(Exception ex)
		{
			_tracer.Error(ex);
			Close(DisconnectedReason.Error, ex.Message);
		}

		void Close(DisconnectedReason reason, string message = null)
		{
			_tracer.Info(Properties.Client_Closing, Id, reason);

			CloseClientSession();
			_packetsSubscription?.Dispose();
			_packetListener?.Dispose();
			ResetReceiver();
			Channel?.Dispose();
			IsConnected = false;
			Id = null;

			Disconnected(this, new MqttEndpointDisconnected(reason, message));
		}

		async Task InitializeChannelAsync()
		{
			Channel = await _channelFactory
				.CreateAsync()
				.ConfigureAwait(continueOnCapturedContext: false);

			_packetListener = new ClientPacketListener(Channel, _flowProvider, _configuration);
			_packetListener.Listen();
			ObservePackets();
		}

		void OpenClientSession(bool cleanSession)
		{
			ClientSession session = !string.IsNullOrEmpty(Id) ? _sessionRepository.Read(Id) : null;
			bool sessionPresent = !cleanSession && session != null;

			if (cleanSession && session != null)
			{
				_sessionRepository.Delete(session.Id);
				session = null;

				_tracer.Info(Properties.Client_CleanedOldSession, Id);
			}
            if( session != null ) return;
            session = new ClientSession(Id, cleanSession);

            _sessionRepository.Create(session);

            _tracer.Info(Properties.Client_CreatedSession, Id);
        }

		void CloseClientSession()
		{
			var session = string.IsNullOrEmpty(Id) ? null : _sessionRepository.Read(Id);

			if (session == null)
			{
				return;
			}

			if (session.Clean)
			{
				_sessionRepository.Delete(session.Id);

				_tracer.Info(Properties.Client_DeletedSessionOnDisconnect, Id);
			}
		}

		async Task SendPacketAsync(IPacket packet)
		{
			await Task.Run(async () => await Channel.SendAsync(packet).ConfigureAwait(continueOnCapturedContext: false))
				.ConfigureAwait(continueOnCapturedContext: false);
		}

		void CheckUnderlyingConnection()
		{
			if (_isProtocolConnected && !Channel.IsConnected)
			{
				Close(DisconnectedReason.Error, Properties.Client_UnexpectedChannelDisconnection);
			}
		}

		void ObservePackets()
		{
			_packetsSubscription = _packetListener
				.PacketStream
				.ObserveOn(NewThreadScheduler.Default)
				.Subscribe(packet =>
				{
                    if( packet.Type != MqttPacketType.Publish ) return;
                    var publish = packet as Publish;
                    var message = new MqttApplicationMessage(publish.Topic, publish.Payload);

                    _receiver.OnNext(message);
                    _tracer.Info(Properties.Client_NewApplicationMessageReceived, Id, publish.Topic);
                },
                    Close,
                    () =>
				{
					_tracer.Warn(Properties.Client_PacketsObservableCompleted);
					Close(DisconnectedReason.RemoteDisconnected);
				});
		}

		void ResetReceiver()
		{
			_receiver?.OnCompleted();
			_receiver = new Subject<MqttApplicationMessage>();
		}
	}
}
