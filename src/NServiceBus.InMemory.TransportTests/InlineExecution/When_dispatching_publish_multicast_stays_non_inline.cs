#nullable enable

namespace NServiceBus.TransportTests;

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_publish_multicast_stays_non_inline
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        broker.Subscribe("input", typeof(MyEvent).FullName!);

        var dispatcher = await CreateDispatcher(broker, ["input"]);
        var task = dispatcher.Dispatch(new TransportOperations(CreateMulticast(typeof(MyEvent))), new TransportTransaction());

        await task;

        Assert.That(task.IsCompletedSuccessfully, Is.True);
        Assert.That(broker.GetOrCreateQueue("input").Count, Is.EqualTo(1));
        Assert.That(GetInlineState(await broker.GetOrCreateQueue("input").Dequeue()), Is.Null);
    }
}

class MyEvent : IEvent;