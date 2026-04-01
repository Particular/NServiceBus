namespace NServiceBus.TransportTests;

using System;
using NUnit.Framework;

[TestFixture]
public class When_simulation_configuration_has_rate_limit_and_direct_limiter
{
    [Test]
    public void Should_fail_when_rate_limit_and_direct_limiter_are_both_configured()
    {
        using var limiter = new CountingRateLimiter(permitCount: 1);

        var exception = Assert.Throws<ArgumentException>(() => new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                RateLimit = new InMemoryRateLimitOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) },
                RateLimiter = limiter
            }
        }));

        Assert.That(exception!.Message, Does.Contain("RateLimiter"));
    }
}