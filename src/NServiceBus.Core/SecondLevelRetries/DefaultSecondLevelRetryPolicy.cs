namespace NServiceBus.SecondLevelRetries
{
    using System;

    class DefaultSecondLevelRetryPolicy:SecondLevelRetryPolicy
    {
        readonly int maxRetries;
        readonly TimeSpan timeIncrease;

        public DefaultSecondLevelRetryPolicy(int maxRetries,TimeSpan timeIncrease)
        {
            this.maxRetries = maxRetries;
            this.timeIncrease = timeIncrease;
        }

        public override bool TryGetDelay(TransportMessage message, Exception ex, int currentRetry, out TimeSpan delay)
        {
            delay = TimeSpan.MinValue;

            if (currentRetry > maxRetries)
            {
                return false;
            }

            if (HasReachedMaxTime(message))
            {
                return false;
            }

            delay = TimeSpan.FromTicks(timeIncrease.Ticks * currentRetry);

            return true;
        }

        static bool HasReachedMaxTime(TransportMessage message)
        {
            string timestampHeader;

            if (!message.Headers.TryGetValue(SecondLevelRetriesBehavior.RetriesTimestamp, out timestampHeader))
            {
                return false;
            }

            
            if (string.IsNullOrEmpty(timestampHeader))
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

        
        public static int DefaultNumberOfRetries = 3;
        public static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    }
}