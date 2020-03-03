using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.MQTT.Client.Abstractions
{
    public class Monitored<T>
    {
        public Monitored(IActivityMonitor monitor, T item)
        {
            Monitor = monitor;
            Item = item;
        }

        public IActivityMonitor Monitor { get; }

        public T Item { get; }
    }
}
