namespace NServiceBus
{
    using System;

    class RateLimitConfiguration
    {
        public Notification<RateLimitStarted> RateLimitStartedNotification { get; } = new Notification<RateLimitStarted>();
        public Notification<RateLimitEnded> RateLimitEndedNotification { get; } = new Notification<RateLimitEnded>();

        public int NumberOfConsecutiveFailuresBeforeRateLimit { get; set; } = int.MaxValue;
        public TimeSpan WaitPeriodBetweenAttempts { get; set; } = TimeSpan.FromSeconds(1);
    }

    class RateLimitStarted
    {
    }

    class RateLimitEnded
    {
    }
}