#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;
using Extensibility;

static class ContextPropagation
{
    public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers, ContextBag contextBag)
    {
        // Removed in v11, see obsolete_v11.cs
        if (!ObsoleteV11.UseDistributedContextPropagator)
        {
            ObsoleteV11.PropagateContextToHeaders(activity, headers, contextBag);
            return;
        }

        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.Inject(activity, headers, Setter);

        var traceParentExists = headers.ContainsKey(Headers.DiagnosticsTraceParent);
        var startNewTraceOnReceive = contextBag.TryGet<string>(Headers.StartNewTrace, out var startNewTrace);

        if (traceParentExists && startNewTraceOnReceive)
        {
            headers[Headers.StartNewTrace] = startNewTrace!;
        }
    }

    public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        // Removed in v11, see obsolete_v11.cs
        if (!ObsoleteV11.UseDistributedContextPropagator)
        {
            ObsoleteV11.PropagateContextFromHeaders(activity, headers);
            return;
        }

        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.ExtractTraceIdAndState(headers, Getter, out _, out var traceState);

        if (traceState is not null)
        {
            activity.TraceStateString = traceState;
        }

        var baggage = DistributedContextPropagator.Current.ExtractBaggage(headers, Getter);

        if (baggage is null)
        {
            return;
        }

        foreach (var baggageItem in baggage)
        {
            activity.AddBaggage(baggageItem.Key, baggageItem.Value);
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