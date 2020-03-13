using CK.Core;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CK.MQTT
{

    /// <summary>
    /// Async event handler that can be combined into a <see cref="SequentialEventHandlerAsyncSender{T}"/>.
    /// </summary>
    /// <param name="monitor">The monitor that must be used to log activities.</param>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object that contains no event data (<see cref="EventArgs.Empty"/> should be used).</param>
    public delegate Task SequentialEventHandlerAync<T>( IActivityMonitor monitor, IMqttClient sender, T e );

    /// <summary>
    /// Implements a host for <see cref="EventHandlerAync"/> delegates.
    /// </summary>
    public class SequentialEventHandlerAsyncSender<T>
    {
        object _handler;

        /// <summary>
        /// Gets whether at least one handler is registered.
        /// </summary>
        public bool HasHandlers => _handler != null;

        /// <summary>
        /// Adds a handler. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="h">Non null handler.</param>
        public SequentialEventHandlerAsyncSender<T> Add( SequentialEventHandlerAync<T> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return handler;
                if( h is SequentialEventHandlerAync<T> a ) return new SequentialEventHandlerAync<T>[] { a, handler };
                var ah = (SequentialEventHandlerAync<T>[])h;
                int len = ah.Length;
                Array.Resize( ref ah, len + 1 );
                ah[len] = handler;
                return ah;
            } );
            return this;
        }

        /// <summary>
        /// Removes a handler if it exists. This is an atomic (thread safe) operation.
        /// </summary>
        /// <param name="h">The handler to remove. Cannot be null.</param>
        public SequentialEventHandlerAsyncSender<T> Remove( SequentialEventHandlerAync<T> handler )
        {
            if( handler == null ) throw new ArgumentNullException( nameof( handler ) );
            Util.InterlockedSet( ref _handler, h =>
            {
                if( h == null ) return null;
                if( h is SequentialEventHandlerAync<T> a ) return a == handler ? null : h;
                var current = (SequentialEventHandlerAync<T>[])h;
                int idx = Array.IndexOf( current, handler );
                if( idx < 0 ) return current;
                Debug.Assert( current.Length > 1 );
                var ah = new SequentialEventHandlerAync<T>[current.Length - 1];
                System.Array.Copy( current, 0, ah, 0, idx );
                System.Array.Copy( current, idx + 1, ah, idx, ah.Length - idx );
                return ah;
            } );
            return this;
        }

        /// <summary>
        /// Relays to <see cref="Add"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to add.</param>
        /// <returns>The host.</returns>
        public static SequentialEventHandlerAsyncSender<T> operator +( SequentialEventHandlerAsyncSender<T> eventHost, SequentialEventHandlerAync<T> handler ) => eventHost.Add( handler );

        /// <summary>
        /// Relays to <see cref="Remove"/>.
        /// </summary>
        /// <param name="eventHost">The host.</param>
        /// <param name="handler">The non null handler to remove.</param>
        /// <returns>The host.</returns>
        public static SequentialEventHandlerAsyncSender<T> operator -( SequentialEventHandlerAsyncSender<T> eventHost, SequentialEventHandlerAync<T> handler ) => eventHost.Remove( handler );

        /// <summary>
        /// Clears the delegate list.
        /// </summary>
        public void RemoveAll() => _handler = null;

        /// <summary>
        /// Raises this event.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event argument.</param>
        public Task RaiseAsync( IActivityMonitor monitor, IMqttClient sender, T args )
        {
            var h = _handler;
            if( h == null ) return Task.CompletedTask;
            if( h is SequentialEventHandlerAync<T> a ) return a( monitor, sender, args );
            return RaiseSequentialAsync( monitor, (SequentialEventHandlerAync<T>[])h, sender, args );
        }

        static async Task RaiseSequentialAsync( IActivityMonitor monitor, SequentialEventHandlerAync<T>[] all, IMqttClient sender, T args )
        {
            foreach( var h in all ) await h( monitor, sender, args );
        }
    }
}
