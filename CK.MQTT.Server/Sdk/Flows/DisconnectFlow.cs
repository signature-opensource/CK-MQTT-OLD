using System.Diagnostics;
using CK.MQTT.Sdk.Packets;
using CK.MQTT.Sdk.Storage;
using System.Threading.Tasks;

namespace CK.MQTT.Sdk.Flows
{
	internal class DisconnectFlow : IProtocolFlow
	{
		static readonly ITracer tracer = Tracer.Get<DisconnectFlow> ();

		readonly IConnectionProvider connectionProvider;
		readonly IRepository<ClientSession> sessionRepository;
		readonly IRepository<ConnectionWill> willRepository;

		public DisconnectFlow (IConnectionProvider connectionProvider,
			IRepository<ClientSession> sessionRepository,
			IRepository<ConnectionWill> willRepository)
		{
			this.connectionProvider = connectionProvider;
			this.sessionRepository = sessionRepository;
			this.willRepository = willRepository;
		}

		public async Task ExecuteAsync (string clientId, IPacket input, IMqttChannel<IPacket> channel)
		{
			if (input.Type != MqttPacketType.Disconnect) {
				return;
			}

			await Task.Run (() => {
				var disconnect = input as Disconnect;

				tracer.Info (ServerProperties.DisconnectFlow_Disconnecting, clientId);

				willRepository.Delete (clientId);

				var session = sessionRepository.Read (clientId);

				if (session == null) {
					throw new MqttException (string.Format (ServerProperties.SessionRepository_ClientSessionNotFound, clientId));
				}

				if (session.Clean) {
					sessionRepository.Delete (session.Id);

					tracer.Info (ServerProperties.Server_DeletedSessionOnDisconnect, clientId);
				}

				connectionProvider.RemoveConnection (clientId);
			});
		}
	}
}
