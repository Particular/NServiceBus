#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

static class DelayedRetriesHeaderExtensions
{
    internal static int GetDelayedDeliveriesPerformed(this Dictionary<string, string> headers)
    {
        if (headers.TryGetValue(Headers.DelayedRetries, out var delayedDeliveriesPerformedHeader) && int.TryParse(delayedDeliveriesPerformedHeader, out var delayedDeliveriesPerformed))
        {
            return delayedDeliveriesPerformed;
        }

        return 0;
    }

    public static int GetDelayedDeliveriesPerformed(this IncomingMessage message) => message.Headers.GetDelayedDeliveriesPerformed();

    public static void SetCurrentDelayedDeliveries(this OutgoingMessage message, int currentDelayedRetry)
    {
        message.Headers[Headers.DelayedRetries] = currentDelayedRetry.ToString();
        if (message.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            message.Headers[Headers.StartNewTrace] = bool.TrueString;
        }
    }

    public static void SetDelayedDeliveryTimestamp(this OutgoingMessage message, DateTimeOffset timestamp) => message.Headers[Headers.DelayedRetriesTimestamp] = DateTimeOffsetHelper.ToWireFormattedString(timestamp);
}