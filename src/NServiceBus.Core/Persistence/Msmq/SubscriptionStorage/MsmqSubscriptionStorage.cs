namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Messaging;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using NServiceBus.Routing;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;
    using MessageType = Unicast.Subscriptions.MessageType;

    class MsmqSubscriptionStorage : IInitializableSubscriptionStorage, IDisposable
    {

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

                entries.Add(new Entry
                {
                    MessageType = messageType,
                    Subscriber = subscriber
                });
                AddToLookup(subscriber.TransportAddress, messageType, m.Id);
            }
        }

        public Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messagelist = messageTypes.ToList();
            lock (locker)
            {
                var result = (from e in entries
                    from m in messagelist
                    where e.MessageType == m
                    select e)
                    .Distinct(EntryComparer)
                    .Select(e => e.Subscriber)
                    .ToArray();

                return Task.FromResult((IEnumerable<Subscriber>)result);
            }
        }

        public Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            lock (locker)
            {
                var found = entries.Any(e =>
                    e.MessageType == messageType &&
                    e.Subscriber.TransportAddress == subscriber.TransportAddress);

                if (!found)
                {
                    Add(subscriber, messageType);

                    var entry = new Entry
                    {
                        MessageType = messageType,
                        Subscriber = subscriber
                    };
                    entries.Add(entry);

                    log.DebugFormat("Subscriber {0} added for message {1}.", subscriber, messageType);
                }
            }
            return TaskEx.CompletedTask;
        }

        public Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            lock (locker)
            {
                var toRemove =
                    from e in entries.ToArray()
                    where e.MessageType == messageType && e.Subscriber.TransportAddress == subscriber.TransportAddress
                    select e;

                foreach (var entry in toRemove)
                {
                    Remove(subscriber.TransportAddress, entry.MessageType);
                    entries.Remove(entry);
                    log.Debug($"Subscriber {subscriber} removed for message {entry.MessageType}.");
                }    
            }
            return TaskEx.CompletedTask;
        }

        void Add(Subscriber subscriber, MessageType messageType)
        {
            var toSend = new Message
            {
                Recoverable = true,
                Label = Serialize(subscriber),
                Body = messageType.TypeName + ", Version=" + messageType.Version
            };

            storageQueue.Send(toSend);

            AddToLookup(subscriber.TransportAddress, messageType, toSend.Id);
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
                ? new EndpointName(parts[1]) 
                : null;

            return new Subscriber(parts[0], endpointName);
        }

        void Remove(string subscriber, MessageType messageType)
        {
            var messageId = RemoveFromLookup(subscriber, messageType);

            if (messageId == null)
            {
                return;
            }

            storageQueue.ReceiveById(messageId);
        }

       
        void AddToLookup(string subscriber, MessageType typeName, string messageId)
        {
            lock (lookup)
            {
                Dictionary<MessageType, string> dictionary;
                if (!lookup.TryGetValue(subscriber, out dictionary))
                {
                    lookup[subscriber] = dictionary = new Dictionary<MessageType, string>();
                }

                if (!dictionary.ContainsKey(typeName))
                {
                    dictionary.Add(typeName, messageId);
                }
            }
        }

        string RemoveFromLookup(string subscriber, MessageType typeName)
        {
            string messageId = null;
            lock (lookup)
            {
                Dictionary<MessageType, string> endpoints;
                if (lookup.TryGetValue(subscriber, out endpoints))
                {
                    if (endpoints.TryGetValue(typeName, out messageId))
                    {
                        endpoints.Remove(typeName);
                        if (endpoints.Count == 0)
                        {
                            lookup.Remove(subscriber);
                        }
                    }
                }
            }
            return messageId;
        }

        public MsmqSubscriptionStorage(IMsmqSubscriptionStorageQueue storageQueue)
        {
            this.storageQueue = storageQueue;
        }

        List<Entry> entries = new List<Entry>();
        object locker = new object();
        IMsmqSubscriptionStorageQueue storageQueue;
        Dictionary<string, Dictionary<MessageType, string>> lookup = new Dictionary<string, Dictionary<MessageType, string>>(StringComparer.OrdinalIgnoreCase);
        static ILog log = LogManager.GetLogger(typeof(ISubscriptionStorage));
        static readonly EntryBySubscriberAddressComparer EntryComparer = new EntryBySubscriberAddressComparer();
        static readonly char[] EntrySeparator = {'|'};

        class EntryBySubscriberAddressComparer : IEqualityComparer<Entry>
        {
            public bool Equals(Entry x, Entry y)
            {
                return x.Subscriber.TransportAddress.Equals(y.Subscriber.TransportAddress, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(Entry obj)
            {
                return obj.Subscriber.TransportAddress.ToLowerInvariant().GetHashCode();
            }
        }
    }
}