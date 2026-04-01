namespace NServiceBus.TransportTests;

using System;
using NUnit.Framework;

[TestFixture]
public class When_simulation_configuration_has_direct_limiter_and_factory
{
    [Test]
    public void Should_fail_when_direct_limiter_and_factory_are_both_configured()
    {
        using var limiter = new CountingRateLimiter(permitCount: 1);

        var exception = Assert.Throws<ArgumentException>(() => new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                RateLimiter = limiter,
                RateLimiterFactory = _ => new CountingRateLimiter(permitCount: 1)
            }
        }));

        Assert.That(exception!.Message, Does.Contain("RateLimiterFactory"));
    }
}