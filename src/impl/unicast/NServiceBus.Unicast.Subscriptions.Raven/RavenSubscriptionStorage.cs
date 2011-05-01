using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Subscriptions.Raven.Indexes;
using Raven.Client;
using RavenDbExceptions = Raven.Http.Exceptions;

namespace NServiceBus.Unicast.Subscriptions.Raven
{
    public class RavenSubscriptionStorage : ISubscriptionStorage
    {
        public IDocumentStore Store { get; set; }

        public string Endpoint { get; set; }

        public void Init()
        {
            new SubscriptionsByMessageType().Execute(Store);
        }

        public void Subscribe(string client, IEnumerable<string> messageTypes)
        {
            var subscriptions = messageTypes.Select(m => new Subscription {
                    Id = Subscription.FormatId(Endpoint, m, client),
                    MessageType = m,
                    Client = client
                }).ToList();

            try
            {
                using (var session = Store.OpenSession())
                {
                    session.Advanced.UseOptimisticConcurrency = true;
                    subscriptions.ForEach(session.Store);
                    session.SaveChanges();
                }
            }
            catch (RavenDbExceptions.ConcurrencyException ex)
            {
                
            }
        }

        public void Unsubscribe(string client, IEnumerable<string> messageTypes)
        {
            var ids = messageTypes
                .Select(m => Subscription.FormatId(Endpoint, m, client))
                .ToList();
            
            using (var session = Store.OpenSession()) {
                ids.ForEach(id => session.Advanced.DatabaseCommands.Delete(id, null));

                session.SaveChanges();
            }
        }

        public IEnumerable<string> GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            using (var session = Store.OpenSession())
            {
                return messageTypes.SelectMany(m => GetSubscribersForMessage(session, m))
                    .Select(s => s.Client)
                    .ToList()
                    .Distinct();
            }
        }

        IEnumerable<Subscription> GetSubscribersForMessage(IDocumentSession session, string messageType)
        {
            var clients = session.Query<Subscription, SubscriptionsByMessageType>()
                .Customize(c => c.WaitForNonStaleResults())
                .Where(s => s.MessageType == messageType);

            return clients;
        }
    }
}
