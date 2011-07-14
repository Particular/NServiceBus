using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using FluentNHibernate;
using NHibernate.Criterion;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage
{
    /// <summary>
    /// Subscription storage using NHibernate for persistence 
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage
    {
        private readonly ISessionSource sessionSource;

        public SubscriptionStorage(ISessionSource sessionSource)
        {
            this.sessionSource = sessionSource;
        }

        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Subscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<string> messageTypes)
        {
            using (var session = sessionSource.CreateSession())
            using(var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                {
                    var subscription = new Subscription
                    {
                        SubscriberEndpoint = address.ToString(),
                        MessageType = messageType
                    };

                    if (session.Get<Subscription>(subscription) == null)
                        session.Save(subscription);
                }

                transaction.Complete();
                session.Flush();
            }
        }

        void ISubscriptionStorage.Unsubscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Unsubscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<string> messageTypes)
        {
            using (var session = sessionSource.CreateSession())
            using (var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                    session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", address, messageType));

                transaction.Complete();
                session.Flush();
            }
        }

        IEnumerable<string> ISubscriptionStorage.GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            return ((ISubscriptionStorage) this).GetSubscriberAddressesForMessage(messageTypes)
                .Select(a => a.ToString());
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<string> messageTypes)
        {
            var subscribers = new List<Address>();

            using (var session = sessionSource.CreateSession())
            {
                subscribers.AddRange(from messageType in messageTypes
                                     from subscription in session.CreateCriteria(typeof (Subscription))
                                                            .Add(Restrictions.Eq("MessageType", messageType))
                                                            .List<Subscription>()
                                     select Address.Parse(subscription.SubscriberEndpoint));
            }

            return subscribers;
        }

        public void Init()
        {
            //No-op
        }
    }
}
