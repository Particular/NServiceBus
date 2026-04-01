namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using NServiceBus.Transport;
using Routing;

static class InMemoryBrokerSimulationTestHelper
{
    public static async Task<IMessageDispatcher> CreateDispatcher(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var infrastructure = await CreateInfrastructure(broker, cancellationToken);
        return infrastructure.Dispatcher;
    }

    public static async Task<IMessageReceiver> CreateReceiver(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var infrastructure = await CreateInfrastructure(broker, cancellationToken);
        return infrastructure.Receivers["main"];
    }

    public static Task<TransportInfrastructure> CreateInfrastructure(InMemoryBroker broker, CancellationToken cancellationToken = default)
    {
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker));
        return transport.Initialize(
            new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true),
            [new ReceiveSettings("main", new QueueAddress("input"), false, true, "error")],
            ["error"],
            cancellationToken);
    }

    public static Task Dispatch(IMessageDispatcher dispatcher, string messageId, string destination, CancellationToken cancellationToken = default)
    {
        var message = new OutgoingMessage(messageId, [], new byte[] { 1 });
        var operation = new TransportOperation(message, new UnicastAddressTag(destination));
        return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), cancellationToken);
    }

    public static BrokerEnvelope CreateEnvelope(string messageId, string destination, long sequenceNumber)
    {
        return BrokerPayloadStore.Borrow(messageId, new byte[] { 1 }, new Dictionary<string, string>(), destination, false, sequenceNumber);
    }
}

sealed class CountingRateLimiter(int permitCount) : RateLimiter
{
    int remainingPermits = permitCount;

    public int AttemptAcquireCalls { get; private set; }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        AttemptAcquireCalls++;
        if (remainingPermits > 0)
        {
            remainingPermits--;
            return SuccessfulLease.Instance;
        }

        return FailedLease.Instance;
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default)
    {
        AttemptAcquireCalls++;
        if (remainingPermits > 0)
        {
            remainingPermits--;
            return ValueTask.FromResult<RateLimitLease>(SuccessfulLease.Instance);
        }

        return ValueTask.FromResult<RateLimitLease>(FailedLease.Instance);
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics GetStatistics() => null;

    protected override void Dispose(bool disposing)
    {
    }

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

    sealed class FailedLease : RateLimitLease
    {
        public static FailedLease Instance { get; } = new();

        public override bool IsAcquired => false;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object metadata)
        {
            metadata = null;
            return false;
        }
    }
}

sealed class ScriptedRateLimiter(params ScriptedRateLimiterStep[] steps) : RateLimiter
{
    readonly Queue<ScriptedRateLimiterStep> scriptedSteps = new(steps);

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        if (scriptedSteps.Count == 0)
        {
            throw new InvalidOperationException("No scripted limiter steps remain.");
        }

        return scriptedSteps.Dequeue().ToLease();
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(AttemptAcquireCore(permitCount));

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics GetStatistics() => null;

    protected override void Dispose(bool disposing)
    {
    }
}

readonly record struct ScriptedRateLimiterStep(bool IsAcquired, TimeSpan? RetryAfter)
{
    public static ScriptedRateLimiterStep Acquired() => new(true, null);

    public static ScriptedRateLimiterStep Rejected(TimeSpan retryAfter) => new(false, retryAfter);

    public RateLimitLease ToLease() => IsAcquired ? SuccessfulScriptedLease.Instance : new RejectedScriptedLease(RetryAfter!.Value);
}

sealed class SuccessfulScriptedLease : RateLimitLease
{
    public static SuccessfulScriptedLease Instance { get; } = new();

    public override bool IsAcquired => true;

    public override IEnumerable<string> MetadataNames => [];

    public override bool TryGetMetadata(string metadataName, out object metadata)
    {
        metadata = null;
        return false;
    }
}

sealed class RejectedScriptedLease(TimeSpan retryAfter) : RateLimitLease
{
    public override bool IsAcquired => false;

    public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

    public override bool TryGetMetadata(string metadataName, out object metadata)
    {
        if (metadataName == MetadataName.RetryAfter.Name)
        {
            metadata = retryAfter;
            return true;
        }

        metadata = null;
        return false;
    }
}
