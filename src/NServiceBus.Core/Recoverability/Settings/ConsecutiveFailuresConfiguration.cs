namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;

    class ConsecutiveFailuresConfiguration
    {
        public int NumberOfConsecutiveFailuresBeforeArming { get; set; } = int.MaxValue;

        public RateLimitSettings RateLimitSettings { get; set; }

        public ConsecutiveFailuresCircuitBreaker CreateCircuitBreaker(Func<Task> onConsecutiveArmed, Func<Task> onConsecutiveDisarmed)
        {
            var timeToWait = TimeSpan.Zero;

            if (RateLimitSettings != null)
            {
                timeToWait = RateLimitSettings.TimeToWaitBetweenThrottledAttempts;
            }

            var consecutiveFailuresCircuitBreaker = new ConsecutiveFailuresCircuitBreaker("System outage circuit breaker", NumberOfConsecutiveFailuresBeforeArming, onConsecutiveArmed, onConsecutiveDisarmed, timeToWait);
            return consecutiveFailuresCircuitBreaker;
        }
    }
}