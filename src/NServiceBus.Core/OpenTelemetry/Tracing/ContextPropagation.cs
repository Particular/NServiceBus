#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;

static class ContextPropagation
{
    public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        // Written on both propagation paths below. The W3C "traceparent" header is still written
        // too, so endpoints on older versions keep reading the context from where they expect it.
        if (activity.Id is not null)
        {
            headers[Headers.NServiceBusDiagnosticsTraceParent] = activity.Id;
        }

        // TODO: investigate if we need to improve the switch check for better performance
        // Removed in v11, see obsolete_v11.cs
        if (!LegacyContextPropagation.UseDistributedContextPropagator)
        {
            LegacyContextPropagation.PropagateContextToHeaders(activity, headers);
            return;
        }

        // The following part was intentionally not extracted to a separate class to prevent
        // accidental leftovers when because that the legacy propagator will be removed in v11 
        DistributedContextPropagator.Current.Inject(activity, headers, Setter);
    }

    public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        PropagateTraceStateFromHeaders(activity, headers);
        PropagateBaggageFromHeaders(activity, headers);
    }

    public static void PropagateTraceStateFromHeaders(Activity activity, IDictionary<string, string> headers)
    {
        // Removed in v11, see obsolete_v11.cs
        if (!LegacyContextPropagation.UseDistributedContextPropagator)
        {
            LegacyContextPropagation.PropagateTraceStateFromHeaders(activity, headers);
            return;
        }

        // The following part was intentionally not extracted to a separate class to prevent
        // accidental leftovers when because that the legacy propagator will be removed in v11 
        DistributedContextPropagator.Current.ExtractTraceIdAndState(headers, Getter, out _, out var traceState);

        if (traceState is not null)
        {
            activity.TraceStateString = traceState;
        }
    }

    // parent: the activity that becomes Activity.Parent once the activity starts, if any. Baggage is
    // read through the parent chain, so items the parent already carries (for example because a
    // transport SDK extracted them from the same message) are not added again. Otherwise every hop
    // would put each key on the wire twice.
    public static void PropagateBaggageFromHeaders(Activity activity, IDictionary<string, string> headers, Activity? parent = null)
    {
        // Removed in v11, see obsolete_v11.cs
        if (!LegacyContextPropagation.UseDistributedContextPropagator)
        {
            LegacyContextPropagation.PropagateBaggageFromHeaders(activity, headers, parent);
            return;
        }

        // The following part was intentionally not extracted to a separate class to prevent
        // accidental leftovers when because that the legacy propagator will be removed in v11 
        var baggage = DistributedContextPropagator.Current.ExtractBaggage(headers, Getter);

        if (baggage is null)
        {
            return;
        }

        foreach (var baggageItem in baggage)
        {
            if (parent?.GetBaggageItem(baggageItem.Key) is null)
            {
                activity.AddBaggage(baggageItem.Key, baggageItem.Value);
            }
        }
    }

    static readonly DistributedContextPropagator.PropagatorSetterCallback Setter = static (carrier, key, value) =>
        ((IDictionary<string, string>)carrier!)[key] = value;

    static readonly DistributedContextPropagator.PropagatorGetterCallback Getter =
        static (carrier, key, out value, out values) =>
        {
            values = null;
            value = ((IReadOnlyDictionary<string, string>)carrier!).GetValueOrDefault(key);
        };
}