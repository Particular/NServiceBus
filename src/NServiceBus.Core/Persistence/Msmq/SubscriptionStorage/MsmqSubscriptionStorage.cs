namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            var messages = storageQueue.GetAllMessages()
                .OrderByDescending(m => m.ArrivedTime)
                .ThenBy(x => x.Id) // ensure same order of messages with same timestamp accross all endpoints
                .ToArray();

            lock (lookup)
            {
                foreach (var m in messages)
                {
                    var messageTypeString = m.Body as string;
                    var messageType = new MessageType(messageTypeString); //this will parse both 2.6 and 3.0 type strings
                    var subscriber = Deserialize(m.Label);

                    Dictionary<MessageType, string> endpointSubscriptions;
                    if (!lookup.TryGetValue(subscriber, out endpointSubscriptions))
                    {
                        lookup[subscriber] = endpointSubscriptions = new Dictionary<MessageType, string>();
                    }

                    if (endpointSubscriptions.ContainsKey(messageType))
                    {
                        // this message is stale and can be removed
                        storageQueue.TryReceiveById(m.Id);
                    }
                    else
                    {
                        endpointSubscriptions[messageType] = m.Id;
                    }
                }
            }
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messagelist = messageTypes.ToArray();
            var result = new HashSet<Subscriber>();

            lock (lookup)
            {
                foreach (var subscribers in lookup)
                {
                    foreach (var messageType in messagelist)
                    {
                        string messageId;
                        if (subscribers.Value.TryGetValue(messageType, out messageId))
                        {
                            result.Add(subscribers.Key);
                        }
                    }
                }
            }

            return Task.FromResult<IEnumerable<Subscriber>>(result);
        }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var body = messageType.TypeName + ", Version=" + messageType.Version;
            var label = Serialize(subscriber);
            var messageId = storageQueue.Send(body, label);

            AddToLookup(subscriber, messageType, messageId);

            return TaskEx.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var messageId = RemoveFromLookup(subscriber, messageType);

            if (messageId != null)
            {
                storageQueue.TryReceiveById(messageId);
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
                    // replace existing subscriber
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
                Dictionary<MessageType, string> subscriptions;
                if (lookup.TryGetValue(subscriber, out subscriptions))
                {
                    string messageId;
                    if (subscriptions.TryGetValue(typeName, out messageId))
                    {
                        subscriptions.Remove(typeName);
                        if (subscriptions.Count == 0)
                        {
                            lookup.Remove(subscriber);
                        }

                        return messageId;
                    }
                }
            }

            return null;
        }

        Dictionary<Subscriber, Dictionary<MessageType, string>> lookup = new Dictionary<Subscriber, Dictionary<MessageType, string>>(SubscriberComparer);
        IMsmqSubscriptionStorageQueue storageQueue;
        static ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static TransportAddressEqualityComparer SubscriberComparer = new TransportAddressEqualityComparer();

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