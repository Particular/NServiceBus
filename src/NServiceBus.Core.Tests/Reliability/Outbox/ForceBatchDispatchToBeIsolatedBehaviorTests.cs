namespace NServiceBus.Core.Tests.Reliability.Outbox;

using System;
using System.Threading.Tasks;
using NServiceBus.Routing;
using NUnit.Framework;
using Testing;
using Transport;

[TestFixture]
public class ForceBatchDispatchToBeIsolatedBehaviorTests
{
    [Test]
    public async Task Should_set_every_operation_dispatch_consistency_to_isolated()
    {
        var behavior = new ForceBatchDispatchToBeIsolatedBehavior();
        var context = new TestableBatchDispatchContext
        {
            Operations =
            [
                CreateOperation(DispatchConsistency.Default),
                CreateOperation(DispatchConsistency.Isolated)
            ]
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Operations, Has.All.Matches<TransportOperation>(o => o.RequiredDispatchConsistency == DispatchConsistency.Isolated));
    }

    [Test]
    public async Task Should_leave_empty_collection_valid_and_still_call_next()
    {
        var behavior = new ForceBatchDispatchToBeIsolatedBehavior();
        var context = new TestableBatchDispatchContext();
        var nextCalled = false;

        await behavior.Invoke(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task Should_overwrite_preexisting_consistency_values()
    {
        var behavior = new ForceBatchDispatchToBeIsolatedBehavior();
        var context = new TestableBatchDispatchContext
        {
            Operations =
            [
                CreateOperation(DispatchConsistency.Default)
            ]
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Operations[0].RequiredDispatchConsistency, Is.EqualTo(DispatchConsistency.Isolated));
    }

    static TransportOperation CreateOperation(DispatchConsistency consistency) =>
        new(
            new OutgoingMessage(Guid.NewGuid().ToString(), [], Array.Empty<byte>()),
            new UnicastAddressTag("destination"),
            requiredDispatchConsistency: consistency);
}
