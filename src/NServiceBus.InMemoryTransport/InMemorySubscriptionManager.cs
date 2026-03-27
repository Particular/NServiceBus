namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Transport;
using Unicast.Messages;

class InMemorySubscriptionManager : ISubscriptionManager
{
    public InMemorySubscriptionManager(InMemoryBroker broker)
    {
        this.broker = broker;
    }

    public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default)
    {
        foreach (var eventType in eventTypes)
        {
            var topic = eventType.MessageType.FullName;
            if (topic != null)
            {
                broker.Subscribe(context.Get<string>(Headers.SubscriberTransportAddress), topic);
            }
        }
        return Task.CompletedTask;
    }

    public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
    {
        var topic = eventType.MessageType.FullName;
        if (topic != null)
        {
            broker.Unsubscribe(context.Get<string>(Headers.SubscriberTransportAddress), topic);
        }
        return Task.CompletedTask;
    }

    readonly InMemoryBroker broker;
}
