using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public readonly struct Mon<T>
    {
        public Mon( IActivityMonitor monitor, T item )
        {
            Monitor = monitor;
            Item = item;
        }

        public IActivityMonitor Monitor { get; }

        public T Item { get; }
    }
}
