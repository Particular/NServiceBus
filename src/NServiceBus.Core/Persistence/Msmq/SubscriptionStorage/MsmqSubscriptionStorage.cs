namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using MessageType = Unicast.Subscriptions.MessageType;

    class MsmqSubscriptionStorage : IInitializableSubscriptionStorage, IDisposable
    {
        public MsmqSubscriptionStorage(IMsmqSubscriptionStorageQueue storageQueue)
        {
            this.storageQueue = storageQueue;
        }

        public void Dispose()
        {
            // Filled in by Janitor.fody
        }

        public void Init()
        {
            foreach (var m in storageQueue.GetAllMessages())
            {
                var messageTypeString = m.Body as string;
                var messageType = new MessageType(messageTypeString); //this will parse both 2.6 and 3.0 type strings
                var subscriber = Deserialize(m.Label);

                AddToLookup(subscriber, messageType, m.Id);
            }
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messagelist = messageTypes.ToList();

            lock (locker)
            {
                var result = new HashSet<Subscriber>(subscriberDeduplicationEqualityComparer);
                foreach (var subscriber in lookup)
                {
                    foreach (var messageType in messagelist)
                    {
                        Tuple<string, Subscriber> subscription;
                        if (subscriber.Value.TryGetValue(messageType, out subscription))
                        {
                            result.Add(subscription.Item2);
                        }
                    }
                }

                return Task.FromResult<IEnumerable<Subscriber>>(result);
            }
        }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            lock (locker)
            {
                Dictionary<MessageType, Tuple<string, Subscriber>> subscriptions;
                if (lookup.TryGetValue(subscriber.TransportAddress, out subscriptions))
                {
                    Tuple<string, Subscriber> existingSubscription;
                    if (subscriptions.TryGetValue(messageType, out existingSubscription))
                    {
                        DeleteSubscription(existingSubscription.Item1, messageType);
                        StoreSubscription(subscriber, messageType);
                        log.Debug($"Subscriber {subscriber.TransportAddress} updated for message {messageType}.");
                    }
                }

                StoreSubscription(subscriber, messageType);
                log.Debug($"Subscriber {subscriber.TransportAddress} added for message {messageType}.");
            }
            return TaskEx.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            lock (locker)
            {
                Dictionary<MessageType, Tuple<string, Subscriber>> subscriptions;
                if (lookup.TryGetValue(subscriber.TransportAddress, out subscriptions))
                {
                    if (subscriptions.ContainsKey(messageType))
                    {
                        DeleteSubscription(subscriber.TransportAddress, messageType);
                        log.Debug($"Subscriber {subscriber.TransportAddress} removed for message {messageType}.");
                    }
                }
            }
            return TaskEx.CompletedTask;
        }

        static string Serialize(Subscriber subscriber)
        {
            return $"{subscriber.TransportAddress}|{subscriber.Endpoint}";
        }

        static Subscriber Deserialize(string serializedForm)
        {
            var parts = serializedForm.Split(EntrySeparator, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || parts.Length > 2)
            {
                log.Error($"Invalid format of subscription entry: {serializedForm}.");
                return null;
            }
            var endpointName = parts.Length > 1
                ? parts[1]
                : null;

            return new Subscriber(parts[0], endpointName);
        }

        void StoreSubscription(Subscriber subscriber, MessageType messageType)
        {
            var toSend = new Message
            {
                Recoverable = true,
                Label = Serialize(subscriber),
                Body = $"{messageType.TypeName}, Version={messageType.Version}"
            };

            storageQueue.Send(toSend);

            AddToLookup(subscriber, messageType, toSend.Id);
        }

        void DeleteSubscription(string subscriber, MessageType messageType)
        {
            var messageId = RemoveFromLookup(subscriber, messageType);

            if (messageId == null)
            {
                return;
            }

            storageQueue.ReceiveById(messageId);
        }

        void AddToLookup(Subscriber subscriber, MessageType typeName, string messageId)
        {
            lock (lookup)
            {
                Dictionary<MessageType, Tuple<string, Subscriber>> dictionary;
                if (!lookup.TryGetValue(subscriber.TransportAddress, out dictionary))
                {
                    lookup[subscriber.TransportAddress] = dictionary = new Dictionary<MessageType, Tuple<string, Subscriber>>();
                }

                dictionary[typeName] = new Tuple<string, Subscriber>(messageId, subscriber);
            }
        }

        string RemoveFromLookup(string subscriber, MessageType typeName)
        {
            lock (lookup)
            {
                Dictionary<MessageType, Tuple<string, Subscriber>> endpoints;
                if (lookup.TryGetValue(subscriber, out endpoints))
                {
                    Tuple<string, Subscriber> subscription;
                    if (endpoints.TryGetValue(typeName, out subscription))
                    {
                        endpoints.Remove(typeName);
                        if (endpoints.Count == 0)
                        {
                            lookup.Remove(subscriber);
                        }
                        return subscription.Item1;
                    }
                }
            }
            return null;
        }

        object locker = new object();
        Dictionary<string, Dictionary<MessageType, Tuple<string, Subscriber>>> lookup = new Dictionary<string, Dictionary<MessageType, Tuple<string, Subscriber>>>(StringComparer.OrdinalIgnoreCase);
        IMsmqSubscriptionStorageQueue storageQueue;
        static ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static SubscriberDeduplicationEqualityComparer subscriberDeduplicationEqualityComparer = new SubscriberDeduplicationEqualityComparer();

        static readonly char[] EntrySeparator =
        {
            '|'
        };

        sealed class SubscriberDeduplicationEqualityComparer : IEqualityComparer<Subscriber>
        {
            public bool Equals(Subscriber x, Subscriber y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.TransportAddress, y.TransportAddress, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Endpoint, y.Endpoint, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Subscriber obj)
            {
                unchecked
                {
                    return ((obj.TransportAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TransportAddress) : 0) * 397) ^ (obj.Endpoint != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Endpoint) : 0);
                }
            }
        }
    }
}