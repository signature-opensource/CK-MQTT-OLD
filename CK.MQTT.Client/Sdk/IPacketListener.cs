
using CK.Core;
using CK.MQTT.Sdk.Packets;
using System;

namespace CK.MQTT.Sdk
{
    internal interface IPacketListener : IDisposable
    {
        IObservable<Mon<IPacket>> PacketStream { get; }

        void Listen();
    }
}
