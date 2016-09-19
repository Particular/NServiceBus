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
                var result = new HashSet<Subscriber>(subscriberEqualityComparer);
                foreach (var subscriber in lookup)
                {
                    foreach (var messageType in messagelist)
                    {
                        string messageId;
                        if (subscriber.Value.TryGetValue(messageType, out messageId))
                        {
                            result.Add(subscriber.Key);
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
                Dictionary<MessageType, string> subscriptions;
                if (lookup.TryGetValue(subscriber, out subscriptions))
                {
                    string messageId;
                    if (subscriptions.TryGetValue(messageType, out messageId))
                    {
                        DeleteSubscription(subscriber, messageType);
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
                Dictionary<MessageType, string> subscriptions;
                if (lookup.TryGetValue(subscriber, out subscriptions))
                {
                    if (subscriptions.ContainsKey(messageType))
                    {
                        DeleteSubscription(subscriber, messageType);
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

        void DeleteSubscription(Subscriber subscriber, MessageType messageType)
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
                Dictionary<MessageType, string> dictionary;
                if (!lookup.TryGetValue(subscriber, out dictionary))
                {
                    dictionary = new Dictionary<MessageType, string>();
                }
                else
                {
                    // replace the existing subscriber with the new one to update endpoint values
                    lookup.Remove(subscriber);
                }

                dictionary[typeName] = messageId;
                lookup[subscriber] = dictionary;
            }
        }

        string RemoveFromLookup(Subscriber subscriber, MessageType typeName)
        {
            lock (lookup)
            {
                Dictionary<MessageType, string> endpoints;
                if (lookup.TryGetValue(subscriber, out endpoints))
                {
                    string messageId;
                    if (endpoints.TryGetValue(typeName, out messageId))
                    {
                        endpoints.Remove(typeName);
                        if (endpoints.Count == 0)
                        {
                            lookup.Remove(subscriber);
                        }
                        return messageId;
                    }
                }
            }
            return null;
        }

        object locker = new object();
        Dictionary<Subscriber, Dictionary<MessageType, string>> lookup = new Dictionary<Subscriber, Dictionary<MessageType, string>>(subscriberEqualityComparer);
        IMsmqSubscriptionStorageQueue storageQueue;
        static ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static TransportAddressEqualityComparer subscriberEqualityComparer = new TransportAddressEqualityComparer();

        static readonly char[] EntrySeparator =
        {
            '|'
        };

        sealed class TransportAddressEqualityComparer : IEqualityComparer<Subscriber>
        {
            public bool Equals(Subscriber x, Subscriber y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.TransportAddress, y.TransportAddress, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Subscriber obj)
            {
                return (obj.TransportAddress != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.TransportAddress) : 0);
            }
        }
    }
}