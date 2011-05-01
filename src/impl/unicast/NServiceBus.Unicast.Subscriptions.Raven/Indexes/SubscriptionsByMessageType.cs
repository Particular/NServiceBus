using System.Linq;
using Raven.Client.Indexes;

namespace NServiceBus.Unicast.Subscriptions.Raven.Indexes
{
    public class SubscriptionsByMessageType : AbstractIndexCreationTask<Subscription>
    {
        public SubscriptionsByMessageType()
        {
            Map = subscriptions => from s in subscriptions select new { s.MessageType };
        }
    }
}