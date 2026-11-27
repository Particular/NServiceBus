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

        // Removed in v11, see obsoletes-v10.cs
        if (!V11BehaviorSwitch.UseV11Behavior)
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
        // Removed in v11, see obsoletes-v10.cs
        if (!V11BehaviorSwitch.UseV11Behavior)
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

    // The activity must be started. Baggage the parent chain already carries (for example because a
    // transport SDK extracted it from the same message onto its receive span) is not added again from
    // the headers. Otherwise every hop would put each key on the wire twice.
    public static void PropagateBaggageFromHeaders(Activity activity, IDictionary<string, string> headers)
    {
        // Removed in v11, see obsoletes-v10.cs
        if (!V11BehaviorSwitch.UseV11Behavior)
        {
            LegacyContextPropagation.PropagateBaggageFromHeaders(activity, headers, activity.Parent);
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
            if (activity.Parent?.GetBaggageItem(baggageItem.Key) is null)
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