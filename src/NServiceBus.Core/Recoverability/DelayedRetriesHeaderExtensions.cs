namespace NServiceBus
{
    using System;
    using Transport;

    static class DelayedRetriesHeaderExtensions
    {
        public static int GetCurrentDelayedRetries(this IncomingMessage message)
        {
            string value;
            if (message.Headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }

            return 0;
        }

        public static void SetCurrentDelayedRetries(this OutgoingMessage message, int currentDelayedRetry)
        {
            message.Headers[Headers.Retries] = currentDelayedRetry.ToString();
        }

        public static void SetDelayedRetryTimestamp(this OutgoingMessage message, DateTime timestamp)
        {
            message.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(timestamp);
        }
    }
}