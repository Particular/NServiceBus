﻿namespace NServiceBus;

using System;
using Transport;

static class DelayedRetriesHeaderExtensions
{
    public static int GetDelayedDeliveriesPerformed(this IncomingMessage message)
    {
        if (message.Headers.TryGetValue(Headers.DelayedRetries, out var value))
        {
            if (int.TryParse(value, out var i))
            {
                return i;
            }
        }

        return 0;
    }

    public static void SetCurrentDelayedDeliveries(this OutgoingMessage message, int currentDelayedRetry)
    {
        message.Headers[Headers.DelayedRetries] = currentDelayedRetry.ToString();
        if (message.Headers.ContainsKey(Headers.DiagnosticsTraceParent))
        {
            message.Headers[Headers.StartNewTrace] = bool.TrueString;
        }
    }

    public static void SetDelayedDeliveryTimestamp(this OutgoingMessage message, DateTimeOffset timestamp)
    {
        message.Headers[Headers.DelayedRetriesTimestamp] = DateTimeOffsetHelper.ToWireFormattedString(timestamp);
    }
}