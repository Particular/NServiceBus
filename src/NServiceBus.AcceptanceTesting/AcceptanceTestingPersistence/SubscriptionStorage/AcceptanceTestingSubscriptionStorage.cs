namespace NServiceBus.AcceptanceTesting
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class AcceptanceTestingSubscriptionStorage : ISubscriptionStorage
    {
        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            var dict = storage.GetOrAdd(messageType, type => new ConcurrentDictionary<string, Subscriber>(StringComparer.OrdinalIgnoreCase));

            dict.AddOrUpdate(subscriber.TransportAddress, _ => subscriber, (_, __) => subscriber);
            return Task.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            if (storage.TryGetValue(messageType, out var dict))
            {
                dict.TryRemove(subscriber.TransportAddress, out var _);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            var result = new HashSet<Subscriber>();
            foreach (var m in messageTypes)
            {
                if (storage.TryGetValue(m, out var list))
                {
                    result.UnionWith(list.Values);
                }
            }
            return Task.FromResult((IEnumerable<Subscriber>)result);
        }

        ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>> storage = new ConcurrentDictionary<MessageType, ConcurrentDictionary<string, Subscriber>>();
    }
}