
using CK.Core;
using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk
{
    internal interface IPacketListener : IDisposable
    {
        IObservable<Monitored<IPacket>> PacketStream { get; }

        void Listen();
    }
}
