using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Simple struct that encapsulate an <see cref="IActivityMonitor"/> around an object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Monitored<T>
    {
        public Monitored(IActivityMonitor activityMonitor, T item)
        {
            ActivityMonitor = activityMonitor;
            Item = item;
        }

        /// <summary>
        /// The activity monitor used to log the life (and tales) of the object.
        /// </summary>
        public readonly IActivityMonitor ActivityMonitor;

        /// <summary>
        /// The item being monitored.
        /// </summary>
        public readonly T Item;
    }
}
