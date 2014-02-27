namespace NServiceBus.SecondLevelRetries
{
    using System;
    using Helpers;


    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0", Message = "Should not be public")]
    public static class DefaultRetryPolicy
    {
        public static int NumberOfRetries = 3;
        public static TimeSpan TimeIncrease = TimeSpan.FromSeconds(10);
        public static Func<TransportMessage, TimeSpan> RetryPolicy = Validate;


        static TimeSpan Validate(TransportMessage message)
        {
            if (HasReachedMaxTime(message))
            {
                return TimeSpan.MinValue;
            }

            var numberOfRetries = TransportMessageHelpers.GetNumberOfRetries(message);

            var timeToIncreaseInTicks = TimeIncrease.Ticks*(numberOfRetries + 1);
            var timeIncrease = TimeSpan.FromTicks(timeToIncreaseInTicks);

            return numberOfRetries >= NumberOfRetries ? TimeSpan.MinValue : timeIncrease;
        }

        static bool HasReachedMaxTime(TransportMessage message)
        {
            var timestampHeader = TransportMessageHelpers.GetHeader(message, SecondLevelRetriesHeaders.RetriesTimestamp);

            if (String.IsNullOrEmpty(timestampHeader))
            {
                return false;
            }

            try
            {
                var handledAt = DateTimeExtensions.ToUtcDateTime(timestampHeader);

                if (DateTime.UtcNow > handledAt.AddDays(1))
                {
                    return true;
                }
            }
            // ReSharper disable once EmptyGeneralCatchClause
            // this code won't usually throw but in case a user has decided to hack a message/headers and for some bizarre reason 
            // they changed the date and that parse fails, we want to make sure that doesn't prevent the message from being 
            // forwarded to the error queue.
            catch (Exception)
            {
            }

            return false;
        }
    }
}