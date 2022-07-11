namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    static class ContextPropagation
    {
        public static void PropagateContextToHeaders(Activity activity, Dictionary<string, string> headers)
        {
            if (activity == null)
            {
                return;
            }

            headers[Headers.DiagnosticsTraceParent] = activity.Id;

            if (activity.TraceStateString != null)
            {
                headers[Headers.DiagnosticsTraceState] = activity.TraceStateString;
            }

            var baggage = string.Join(",", activity.Baggage.Select(item => $"{item.Key}={Uri.EscapeDataString(item.Value)}"));
            if (!string.IsNullOrEmpty(baggage))
            {
                headers[Headers.DiagnosticsBaggage] = baggage;
            }
        }

        public static void PropagateContextFromHeaders(Activity activity, IDictionary<string, string> headers)
        {
            if (activity == null)
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
                    var baggageItem = baggageItems[i];
                    var firstEquals = baggageItem.IndexOf('=');
                    if (firstEquals >= 0 && firstEquals < baggageItem.Length)
                    {
                        var key = baggageItem.Substring(0, firstEquals).Trim();
                        var value = baggageItem.Substring(firstEquals + 1);
                        activity.AddBaggage(key, Uri.UnescapeDataString(value));
                    }
                }
            }
        }
    }
}