namespace NServiceBus
{
    using System;
    using Transports;

    class DefaultSecondLevelRetryPolicy : SecondLevelRetryPolicy
    {
        public DefaultSecondLevelRetryPolicy(int maxRetries, TimeSpan timeIncrease)
            : this(maxRetries, timeIncrease, () => DateTime.UtcNow)
        {
        }

        internal DefaultSecondLevelRetryPolicy(int maxRetries, TimeSpan timeIncrease, Func<DateTime> currentUtcTimeProvider)
        {
            Guard.AgainstNull(nameof(currentUtcTimeProvider), currentUtcTimeProvider);

            this.maxRetries = maxRetries;
            this.timeIncrease = timeIncrease;
            this.currentUtcTimeProvider = currentUtcTimeProvider;
        }

        public override bool TryGetDelay(IncomingMessage message, Exception ex, int currentRetry, out TimeSpan delay)
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

            delay = TimeSpan.FromTicks(timeIncrease.Ticks*currentRetry);

            return true;
        }

        private bool HasReachedMaxTime(IncomingMessage message)
        {
            string timestampHeader;

            if (!message.Headers.TryGetValue(Headers.RetriesTimestamp, out timestampHeader))
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

                var now = currentUtcTimeProvider();
                if (now > handledAt.AddDays(1))
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

        Func<DateTime> currentUtcTimeProvider;
        int maxRetries;
        TimeSpan timeIncrease;


        public static int DefaultNumberOfRetries = 3;
        public static TimeSpan DefaultTimeIncrease = TimeSpan.FromSeconds(10);
    }
}