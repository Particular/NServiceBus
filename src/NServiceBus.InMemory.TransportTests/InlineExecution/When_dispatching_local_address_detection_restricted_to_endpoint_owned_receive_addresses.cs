#nullable enable

namespace NServiceBus.TransportTests;

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_local_address_detection_restricted_to_endpoint_owned_receive_addresses
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var dispatcher = await InlineExecutionTestHelper.CreateDispatcher(broker, ["input", "input-instance-a"]);

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("error")), new TransportTransaction());

        await task;

        Assert.That(task.IsCompletedSuccessfully, Is.True);
        Assert.That(InlineExecutionTestHelper.GetInlineState(await broker.GetOrCreateQueue("error").Dequeue()), Is.Null);
    }
}