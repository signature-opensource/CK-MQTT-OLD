using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace CK.MQTT.Client.netfx
{
    public static class ObservableExtensions
    {
        public static IObservable<(TAOut, TBOut)> OfType<TAOut, TBOut, TAIn, TBIn>( this IObservable<(TAIn, TBIn)> @this )
            where TAOut : TAIn
            where TBOut : TBIn
            =>
            @this.Where( p => p.Item1 is TAOut && p.Item2 is TBOut )
            .Select( s => ((TAOut)s.Item1, (TBOut)s.Item2) );
    }
}
