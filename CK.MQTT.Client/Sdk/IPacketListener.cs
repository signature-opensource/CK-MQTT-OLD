using CK.MQTT.Sdk.Packets;

namespace CK.MQTT.Sdk
{
	internal interface IPacketListener : IDisposable
	{
		IObservable<IPacket> PacketStream { get; }

		void Listen ();
	}
}
