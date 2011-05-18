using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Transactions;
using NHibernate.Criterion;
using NHibernate.Transform;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    /// <summary>
    /// Subscription storage using NHibernate for persistence 
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage
    {
        private readonly ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider;

        public SubscriptionStorage(ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider)
        {
            this.subscriptionStorageSessionProvider = subscriptionStorageSessionProvider;
        }

        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Subscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<string> messageTypes)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var session = subscriptionStorageSessionProvider.OpenSession())
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
            }
        }

        void ISubscriptionStorage.Unsubscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Unsubscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<string> messageTypes)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                foreach (var messageType in messageTypes)
                    session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", address, messageType));

                transaction.Complete();
            }
        }

        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<string> messageTypes)
        {
            return GetSubscribersForMessage(messageTypes).Select(s => Address.Parse(s));
        }

        public IEnumerable<string> GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = subscriptionStorageSessionProvider.OpenStatelessSession())
                return session.CreateCriteria(typeof(Subscription))
                    .Add(Restrictions.In("MessageType", messageTypes.ToArray()))
                    .SetProjection(Projections.Property("SubscriberEndpoint"))
                    .SetResultTransformer(new DistinctRootEntityResultTransformer())
                    .List<string>();
        }

        public void Init()
        {
            //No-op
        }
    }
}