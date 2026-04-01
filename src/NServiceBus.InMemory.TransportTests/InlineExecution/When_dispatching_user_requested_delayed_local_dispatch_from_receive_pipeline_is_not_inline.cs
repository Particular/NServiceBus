#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_user_requested_delayed_local_dispatch_from_receive_pipeline_is_not_inline
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var dispatcher = await CreateDispatcher(broker, ["input"]);
        var transaction = new TransportTransaction();
        var receiveTransaction = CreateReceiveTransaction();
        var scope = CreateScope();

        transaction.Set(CreateReceivePipelineMarker());
        AttachReceiveTransaction(transaction, receiveTransaction);
        AttachInlineScope(transaction, scope);

        var task = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input", DispatchConsistency.Default, TimeSpan.FromMinutes(1))), transaction);

        await task;

        var pending = GetPendingEnvelopes(receiveTransaction);

        Assert.Multiple(() =>
        {
            Assert.That(task.IsCompletedSuccessfully, Is.True);
            Assert.That(task, Is.Not.SameAs(GetCompletion(scope)));
            Assert.That(pending, Has.Count.EqualTo(1));
            Assert.That(GetInlineState(pending.Single()), Is.Null);
        });
    }
}
