namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class When_disposing_broker_with_rate_limiter_factory
{
    [Test]
    public async Task Should_dispose_factory_created_limiters()
    {
        var limiter = new DisposableRateLimiter();
        var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            Send =
            {
                RateLimiterFactory = _ => limiter
            }
        });

        var dispatcher = await InMemoryBrokerSimulationTestHelper.CreateDispatcher(broker);
        await InMemoryBrokerSimulationTestHelper.Dispatch(dispatcher, "msg-1", "queue");

        await broker.DisposeAsync();

        Assert.That(limiter.Disposed, Is.True);
    }
}

sealed class DisposableRateLimiter : RateLimiter
{
    public bool Disposed { get; private set; }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) => SuccessfulLease.Instance;

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<RateLimitLease>(SuccessfulLease.Instance);

    protected override void Dispose(bool disposing) => Disposed = true;

    sealed class SuccessfulLease : RateLimitLease
    {
        public static SuccessfulLease Instance { get; } = new();

        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object metadata)
        {
            metadata = null;
            return false;
        }
    }
}
