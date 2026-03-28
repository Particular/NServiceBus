namespace NServiceBus.Persistence.InMemory.SubscriptionStorage;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Unicast.Subscriptions;
using Unicast.Subscriptions.MessageDrivenSubscriptions;

class InMemorySubscriptionStorage(InMemoryStorage storage) : ISubscriptionStorage
{
    public InMemorySubscriptionStorage() : this(new InMemoryStorage())
    {
    }

    public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
    {
        var subscribers = storage.GetOrAdd(messageType, _ => new ConcurrentDictionary<string, Subscriber>(StringComparer.OrdinalIgnoreCase));
        subscribers.AddOrUpdate(subscriber.TransportAddress, _ => subscriber, (_, __) => subscriber);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
    {
        if (storage.TryGetValue(messageType, out var subscribers))
        {
            subscribers.TryRemove(subscriber.TransportAddress, out _);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
    {
        var subscribers = messageTypes
            .SelectMany(msgType => storage.TryGetValue(msgType, out var subs) ? subs.Values : [])
            .GroupBy(s => new { s.TransportAddress, s.Endpoint })
            .Select(g => g.First());

        return Task.FromResult(subscribers);
    }

    readonly ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> storage = storage.Subscriptions;
}
