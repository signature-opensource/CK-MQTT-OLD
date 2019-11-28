using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CK.MQTT;
using CK.MQTT.Sdk;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ConsoleApp1
{
	internal class GenericChannel : IMqttChannel<byte[]>
	{
		static readonly ITracer tracer = Tracer.Get<GenericChannel>();

		bool disposed;

		readonly IChannelClient client;
		readonly IPacketBuffer buffer;
		readonly ReplaySubject<byte[]> receiver;
		readonly ReplaySubject<byte[]> sender;
		readonly IDisposable streamSubscription;

		public GenericChannel(
			IChannelClient client,
			IPacketBuffer buffer,
			MqttConfiguration configuration)
		{
			this.client = client;
			this.client.PreferedReceiveBufferSize = configuration.BufferSize;
			this.client.PreferedSendBufferSize = configuration.BufferSize;
			this.buffer = buffer;
			receiver = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			sender = new ReplaySubject<byte[]>(window: TimeSpan.FromSeconds(configuration.WaitTimeoutSecs));
			streamSubscription = SubscribeStream();
		}

		public bool IsConnected
		{
			get
			{
				var connected = !disposed;

				try
				{
					connected = connected && client.Connected;
				}
				catch (Exception)
				{
					connected = false;
				}

				return connected;
			}
		}

		public IObservable<byte[]> ReceiverStream { get { return receiver; } }

		public IObservable<byte[]> SenderStream { get { return sender; } }

		public async Task SendAsync(byte[] message)
		{
			if (disposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			if (!IsConnected)
			{
				throw new MqttException("The underlying communication stream is not connected");
			}

			sender.OnNext(message);

			try
			{
				tracer.Verbose("Sending packet of {0} bytes", message.Length);

				await client.GetStream()
					.WriteAsync(message, 0, message.Length)
					.ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (ObjectDisposedException disposedEx)
			{
				throw new MqttException("The underlying communication stream is not available. The socket could have been disconnected", disposedEx);
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;

			if (disposing)
			{
				tracer.Info("Disposing {0}...", GetType().FullName);

				streamSubscription.Dispose();
				receiver.OnCompleted();

				try
				{
					client?.Dispose();
				}
				catch (SocketException socketEx)
				{
					tracer.Error(socketEx, "An error occurred while closing underlying communication channel. Error code: {0}", socketEx.SocketErrorCode);
				}

				disposed = true;
			}
		}

		IDisposable SubscribeStream()
		{
			return Observable.Defer(() => {
				var buffer = new byte[client.PreferedReceiveBufferSize];

				return Observable.FromAsync<int>(() => {
					return client.GetStream().ReadAsync(buffer, 0, buffer.Length);
				})
				.Select(x => buffer.Take(x));
			})
			.Repeat()
			.TakeWhile(bytes => bytes.Any())
			.ObserveOn(NewThreadScheduler.Default)
			.Subscribe(bytes => {
				var packets = default(IEnumerable<byte[]>);

				if (buffer.TryGetPackets(bytes, out packets))
				{
					foreach (var packet in packets)
					{
						tracer.Verbose("Received packet of {0} bytes", packet.Length);

						receiver.OnNext(packet);
					}
				}
			}, ex => {
				if (ex is ObjectDisposedException)
				{
					receiver.OnError(new MqttException("The underlying communication stream is not available. The socket could have been disconnected", ex));
				}
				else
				{
					receiver.OnError(ex);
				}
			}, () => {
				tracer.Warn("The underlying communication stream has completed sending bytes. The observable sequence will be completed and the channel will be disposed");
				receiver.OnCompleted();
			});
		}
	}
}
