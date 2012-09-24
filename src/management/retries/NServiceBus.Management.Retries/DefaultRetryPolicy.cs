using System;
using NServiceBus.Management.Retries.Helpers;

namespace NServiceBus.Management.Retries
{
    public static class DefaultRetryPolicy
    {
        public static int NumberOfRetries = 3;
        public static TimeSpan TimeIncrease = TimeSpan.FromSeconds(10);

        public static TimeSpan Validate(TransportMessage message)
        {
            var numberOfRetries = TransportMessageHelpers.GetNumberOfRetries(message);

            var timeToIncreaseInTicks = TimeIncrease.Ticks*(numberOfRetries + 1);
            var timeIncrease = TimeSpan.FromTicks(timeToIncreaseInTicks);

            return numberOfRetries >= NumberOfRetries ? TimeSpan.MinValue : timeIncrease;
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