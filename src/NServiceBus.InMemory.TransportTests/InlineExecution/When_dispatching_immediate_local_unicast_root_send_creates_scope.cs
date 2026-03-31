#nullable enable

namespace NServiceBus.TransportTests;

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_immediate_local_unicast_root_send_creates_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var dispatcher = await InlineExecutionTestHelper.CreateDispatcher(broker, ["input"]);

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());
        var envelope = await broker.GetOrCreateQueue("input").Dequeue();
        var inlineState = InlineExecutionTestHelper.GetInlineState(envelope);
        var scope = InlineExecutionTestHelper.GetScope(inlineState!);

        Assert.Multiple(() =>
        {
            Assert.That(task.IsCompleted, Is.False);
            Assert.That(inlineState, Is.Not.Null);
            Assert.That(InlineExecutionTestHelper.GetIsRootDispatch(inlineState!), Is.True);
            Assert.That(InlineExecutionTestHelper.GetDepth(inlineState!), Is.EqualTo(0));
            Assert.That(task, Is.SameAs(InlineExecutionTestHelper.GetCompletion(scope)));
        });
    }
}