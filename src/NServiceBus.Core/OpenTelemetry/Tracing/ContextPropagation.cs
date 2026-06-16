#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;
using Extensibility;

static class ContextPropagation
{
    public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers, ContextBag contextBag)
    {
        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.Inject(activity, headers, static (carrier, fieldName, fieldValue) =>
        {
            ((Dictionary<string, string>)carrier!)[fieldName] = fieldValue;
        });

        var traceParentExists = headers.ContainsKey(Headers.DiagnosticsTraceParent);
        var startNewTraceOnReceive = contextBag.TryGet<string>(Headers.StartNewTrace, out var startNewTrace);

        if (traceParentExists && startNewTraceOnReceive)
        {
            headers[Headers.StartNewTrace] = startNewTrace!;
        }
    }

    public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        DistributedContextPropagator.Current.ExtractTraceIdAndState(
            headers,
            static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
            {
                fieldValues = null;
                ((IDictionary<string, string>)carrier!).TryGetValue(fieldName, out fieldValue);
            },
            out _,
            out var traceState);

        if (traceState is not null)
        {
            activity.TraceStateString = traceState;
        }

        var baggage = DistributedContextPropagator.Current.ExtractBaggage(headers, Getter);

        if (baggage is null)
        {
            return;
        }

        foreach (var baggageItem in DistributedContextPropagator.Current.ExtractBaggage(
                     headers,
                     static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                     {
                         fieldValues = null;
                         ((IDictionary<string, string>)carrier!).TryGetValue(fieldName, out fieldValue);
                     })!)
        {
            activity.AddBaggage(baggageItem.Key, baggageItem.Value);
        }
    }

    static readonly DistributedContextPropagator.PropagatorSetterCallback Setter = static (carrier, key, value) =>
        ((IDictionary<string, string>)carrier!)[key] = value;

    static readonly DistributedContextPropagator.PropagatorGetterCallback Getter =
        static (object? carrier, string key, out string? value, out IEnumerable<string>? values) =>
        {
            values = null;
            value = ((IReadOnlyDictionary<string, string>)carrier!).TryGetValue(key, out var v) ? v : null;
        };
}