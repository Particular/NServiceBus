#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_delayed_local_root_send_outside_pipeline_creates_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var dispatcher = await InlineExecutionTestHelper.CreateDispatcher(broker, ["input"]);

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", delay: TimeSpan.FromMinutes(1))), new TransportTransaction());

        Assert.That(task.IsCompleted, Is.False);

        Assert.That(broker.TryDequeueDelayed(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2), out var envelope), Is.True);
        var inlineState = InlineExecutionTestHelper.GetInlineState(envelope!);

        Assert.Multiple(() =>
        {
            Assert.That(inlineState, Is.Not.Null);
            Assert.That(InlineExecutionTestHelper.GetIsRootDispatch(inlineState!), Is.True);
            Assert.That(task, Is.SameAs(InlineExecutionTestHelper.GetCompletion(InlineExecutionTestHelper.GetScope(inlineState!))));
        });
    }
}