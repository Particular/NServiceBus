namespace NServiceBus.Persistence.InMemory.SubscriptionStorage;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        using var activity = InMemoryPersistenceTracing.StartSubscriptionSubscribe(subscriber, messageType);
        var subscribers = storage.GetOrAdd(messageType, static _ => new ConcurrentDictionary<string, Subscriber>(StringComparer.OrdinalIgnoreCase));
        subscribers[subscriber.TransportAddress] = subscriber;
        InMemoryPersistenceTracing.MarkSuccess(activity);
        return Task.CompletedTask;
    }

    public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
    {
        using var activity = InMemoryPersistenceTracing.StartSubscriptionUnsubscribe(subscriber, messageType);
        if (storage.TryGetValue(messageType, out var subscribers))
        {
            subscribers.TryRemove(subscriber.TransportAddress, out _);
        }
        InMemoryPersistenceTracing.MarkSuccess(activity);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
    {
        using var activity = InMemoryPersistenceTracing.StartGetSubscribers(messageTypes);
        Dictionary<(string TransportAddress, string Endpoint), Subscriber> subscribers = [];

        foreach (var messageType in messageTypes)
        {
            if (!storage.TryGetValue(messageType, out var subscriptions))
            {
                continue;
            }

            foreach (var subscriber in subscriptions.Values)
            {
                subscribers[(subscriber.TransportAddress, subscriber.Endpoint)] = subscriber;
            }
        }

        InMemoryPersistenceTracing.AddResolvedSubscribersEvent(activity, subscribers.Count);
        InMemoryPersistenceTracing.MarkSuccess(activity);
        return Task.FromResult<IEnumerable<Subscriber>>(subscribers.Values);
    }

    readonly ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> storage = storage.Subscriptions;
}
