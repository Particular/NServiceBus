#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class ConsecutiveFailuresConfiguration
{
    public int NumberOfConsecutiveFailuresBeforeArming { get; set; } = int.MaxValue;

    public RateLimitSettings? RateLimitSettings { get; set; }

    public ConsecutiveFailuresCircuitBreaker CreateCircuitBreaker(Func<long, CancellationToken, Task> onConsecutiveArmed, Func<long, CancellationToken, Task> onConsecutiveDisarmed)
    {
        var timeToWait = RateLimitSettings?.TimeToWaitBetweenThrottledAttempts ?? TimeSpan.Zero;

        var consecutiveFailuresCircuitBreaker = new ConsecutiveFailuresCircuitBreaker("System outage circuit breaker", NumberOfConsecutiveFailuresBeforeArming, onConsecutiveArmed, onConsecutiveDisarmed, timeToWait);
        return consecutiveFailuresCircuitBreaker;
    }
}