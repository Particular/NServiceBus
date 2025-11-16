#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Extensibility;

static class ContextPropagation
{
    public static void PropagateContextToHeaders(Activity? activity, Dictionary<string, string> headers, ContextBag contextBag)
    {
        if (activity is null)
        {
            return;
        }

        if (activity.Id is not null)
        {
            headers[Headers.DiagnosticsTraceParent] = activity.Id;
        }

        if (activity.TraceStateString is not null)
        {
            headers[Headers.DiagnosticsTraceState] = activity.TraceStateString;
        }

        // Check whether the startnewtrace setting was set in the context, if so, add it to the headers now the trace parent was added
        if (contextBag.TryGet<string>(Headers.StartNewTrace, out var headerContent))
        {
            headers[Headers.StartNewTrace] = headerContent;
        }

        var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value ?? string.Empty)}"));
        if (!string.IsNullOrEmpty(baggage))
        {
            headers[Headers.DiagnosticsBaggage] = baggage;
        }
    }

    public static void PropagateContextFromHeaders(Activity? activity, IDictionary<string, string> headers)
    {
        if (activity is null)
        {
            return;
        }

        if (headers.TryGetValue(Headers.DiagnosticsTraceState, out var traceState))
        {
            activity.TraceStateString = traceState;
        }

        if (headers.TryGetValue(Headers.DiagnosticsBaggage, out var baggageValue))
        {
            var baggageItems = baggageValue.Split(',');
            // HINT: Iterate in reverse order because Activity baggage is LIFO
            for (var i = baggageItems.Length - 1; i >= 0; i--)
            {
                var baggageItem = baggageItems[i].AsSpan();
                var firstEquals = baggageItem.IndexOf('=');
                if (firstEquals >= 0 && firstEquals < baggageItem.Length)
                {
                    var key = baggageItem[..firstEquals].Trim();
                    var value = baggageItem[(firstEquals + 1)..];
                    activity.AddBaggage(key.ToString(), Uri.UnescapeDataString(value));
                }
            }
        }
    }
}