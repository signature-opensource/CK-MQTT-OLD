using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk
{
	internal interface IPacketListener : IDisposable
	{
		IObservable<IPacket> PacketStream { get; }

		void Listen ();
	}
}
