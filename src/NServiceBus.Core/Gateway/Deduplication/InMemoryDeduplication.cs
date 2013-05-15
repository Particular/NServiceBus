namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class InMemoryDeduplication : IDeduplicateMessages
    {
        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            lock (persistence)
            {
                var item = persistence.SingleOrDefault(m => m.Id == clientId);
                if (item != null)
                    return false;

                return persistence.Add(new MessageData(clientId, timeReceived));
            }
        }

        private class MessageData
        {
            public MessageData(string id, DateTime received)
            {
                Id = id;
                Received = received;
            }

            public string Id;
            public DateTime Received;
        }

        private class MessageDataComparer : IEqualityComparer<MessageData>
        {
            public bool Equals(MessageData x, MessageData y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(MessageData obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        readonly ISet<MessageData> persistence = new HashSet<MessageData>(new MessageDataComparer());
    }
}