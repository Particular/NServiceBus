namespace NServiceBus.Gateway.Deduplication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class InMemoryGatewayDeduplication : IDeduplicateMessages
    {
        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            lock (persistence)
            {
                var item = persistence.SingleOrDefault(m => m.Id == clientId);
                if (item != null)
                    return false;

                return persistence.Add(new GatewayMessage { Id = clientId, TimeReceived = timeReceived });
            }
        }

        class MessageDataComparer : IEqualityComparer<GatewayMessage>
        {
            public bool Equals(GatewayMessage x, GatewayMessage y)
            {
                return x.Id == y.Id;
            }

            public int GetHashCode(GatewayMessage obj)
            {
                return obj.Id.GetHashCode();
            }
        }

        ISet<GatewayMessage> persistence = new HashSet<GatewayMessage>(new MessageDataComparer());

        public int DeleteDeliveredMessages(DateTime until)
        {
            int count;
            lock (persistence)
            {
                var items = persistence.Where(msg => msg.TimeReceived <= until).ToList();
                count = items.Count();

                items.ForEach(item => persistence.Remove(item));
            }
            return count;
        }
        class GatewayMessage
        {
            public string Id { get; set; }
            public DateTime TimeReceived { get; set; }
        }
    }
}