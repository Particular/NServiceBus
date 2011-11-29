using System.Collections.Generic;
using System.Linq;
using Raven.Client;

namespace NServiceBus.Unicast.Subscriptions.Raven
{
    using global::Raven.Abstractions.Exceptions;

    public class RavenSubscriptionStorage : ISubscriptionStorage
    {
        public IDocumentStore Store { get; set; }

        public string Endpoint { get; set; }

        public void Init()
        {
        }

        void ISubscriptionStorage.Subscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            var subscriptions = messageTypes.Select(m => new Subscription
            {
                Id = Subscription.FormatId(Endpoint, m, client.ToString()),
                MessageType = m,
                Client = client
            }).ToList();

            try
            {
                using (var session = OpenSession())
                {
                    session.Advanced.UseOptimisticConcurrency = true;
                    subscriptions.ForEach(session.Store);
                    session.SaveChanges();
                }
            }
            catch (ConcurrencyException ex)
            {

            }
        }

       

        void ISubscriptionStorage.Unsubscribe(Address client, IEnumerable<MessageType> messageTypes)
        {
            var ids = messageTypes
                .Select(m => Subscription.FormatId(Endpoint, m, client.ToString()))
                .ToList();

            using (var session = OpenSession())
            {
                ids.ForEach(id => session.Advanced.DatabaseCommands.Delete(id, null));

                session.SaveChanges();
            }
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            using (var session = OpenSession())
            {
                return messageTypes.SelectMany(m => GetSubscribersForMessage(session, m))
                                .Where(s => messageTypes.Contains(s.MessageType))
                                    .Select(s => s.Client)
                                    .ToList()
                                    .Distinct();
            }
        }

        IEnumerable<Subscription> GetSubscribersForMessage(IDocumentSession session, MessageType messageType)
        {
            return session.Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .Where(s => s.MessageType == messageType);
        }

        IDocumentSession OpenSession()
        {
            if(string.IsNullOrEmpty(Endpoint))
            return Store.OpenSession();


            return Store.OpenSession(Endpoint);
        }
    }
}
