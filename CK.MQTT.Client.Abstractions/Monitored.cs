using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public interface IMonitored<out T>
    {
        IActivityMonitor Monitor { get; }
        T Item { get; }
    }

    public static class MonitoredExtenstions
    {
        public static IObservable<IMonitored<TOut>> OfMonitoredType<TOut>( this IObservable<IMonitored<object>> @this )
            => @this.Where( p => p.Item is TOut )
                .Select( s => Unsafe.As<IMonitored<TOut>>( s ) );
    }

    public class Monitored<T> : IMonitored<T>
    {
        Monitored( IActivityMonitor monitor, T item )
        {
            Monitor = monitor;
            Item = item;
        }

        public static IMonitored<T> Create( IActivityMonitor m, T item ) => new Monitored<T>( m, item );

        public IActivityMonitor Monitor { get; }

        public T Item { get; }
    }
}
