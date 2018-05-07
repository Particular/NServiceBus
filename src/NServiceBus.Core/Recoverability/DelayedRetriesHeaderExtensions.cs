namespace NServiceBus
{
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
        }

        public static void SetDelayedDeliveryTimestamp(this OutgoingMessage message, DateTime timestamp)
        {
            message.Headers[Headers.DelayedRetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(timestamp);
        }
    }
}