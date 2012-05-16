using System;
using NServiceBus.Management.Retries.Helpers;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Management.Retries
{
    public static class DefaultRetryPolicy
    {
        const int MAX_NUM_RETRIES = 10;

        public static TimeSpan Validate(TransportMessage message)
        {
            var numberOfRetries = TransportMessageHelpers.GetNumberOfRetries(message);
            return numberOfRetries >= MAX_NUM_RETRIES ? TimeSpan.MinValue : TimeSpan.FromMinutes((numberOfRetries + 1)*5);
        }

        public static bool HasTimedOut(TransportMessage message)
        {
            var timestampHeader = TransportMessageHelpers.GetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp);
            try
            {
                var handledAt = timestampHeader.ToUtcDateTime();

                if (DateTime.UtcNow > handledAt.AddDays(1))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }
    }
}