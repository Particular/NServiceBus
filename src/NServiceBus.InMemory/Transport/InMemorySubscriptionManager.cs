namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Transport;
using Unicast.Messages;

class InMemorySubscriptionManager(InMemoryBroker broker, string localAddress) : ISubscriptionManager
{
    public Task SubscribeAll(MessageMetadata[] eventTypes, ContextBag context, CancellationToken cancellationToken = default)
    {
        foreach (var eventType in eventTypes)
        {
            var topic = eventType.MessageType.FullName;
            if (topic != null)
            {
                broker.Subscribe(localAddress, topic);
            }
        }
        return Task.CompletedTask;
    }

    public Task Unsubscribe(MessageMetadata eventType, ContextBag context, CancellationToken cancellationToken = default)
    {
        var topic = eventType.MessageType.FullName;
        if (topic != null)
        {
            broker.Unsubscribe(localAddress, topic);
        }
        return Task.CompletedTask;
    }
}
