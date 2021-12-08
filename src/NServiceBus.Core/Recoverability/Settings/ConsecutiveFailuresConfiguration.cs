namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class ConsecutiveFailuresConfiguration
    {
        public Notification<ConsecutiveFailuresArmed> ConsecutiveFailuresArmedNotification { get; } = new Notification<ConsecutiveFailuresArmed>();
        public Notification<ConsecutiveFailuresDisarmed> ConsecutiveFailuresDisarmedNotification { get; } = new Notification<ConsecutiveFailuresDisarmed>();

        public int NumberOfConsecutiveFailuresBeforeArming { get; set; } = int.MaxValue;

        public RateLimitSettings RateLimitSettings { get; set; }

        public ConsecutiveFailuresCircuitBreaker CreateCircuitBreaker()
        {
            var onConsecutiveArmed = noopTask;
            var onConsecutiveDisarmed = noopTask;
            var timeToWait = TimeSpan.Zero;

            if (RateLimitSettings != null)
            {
                onConsecutiveArmed = RateLimitSettings.OnRateLimitStarted;
                onConsecutiveDisarmed = RateLimitSettings.OnRateLimitEnded;
                timeToWait = RateLimitSettings.TimeToWaitBetweenThrottledAttempts;
            }

            var consecutiveFailuresCircuitBreaker = new ConsecutiveFailuresCircuitBreaker("System outage circuit breaker", NumberOfConsecutiveFailuresBeforeArming, onConsecutiveArmed, onConsecutiveDisarmed, timeToWait);
            return consecutiveFailuresCircuitBreaker;
        }

        static Func<Task> noopTask = () => TaskEx.CompletedTask;
    }



    class ConsecutiveFailuresArmed
    {
    }

    class ConsecutiveFailuresDisarmed
    {
    }
}