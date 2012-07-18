using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NHibernate.Criterion;
using NServiceBus.Logging;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    /// <summary>
    /// Subscription storage using NHibernate for persistence 
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage
    {
        readonly ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider;

        public SubscriptionStorage(ISubscriptionStorageSessionProvider subscriptionStorageSessionProvider)
        {
            this.subscriptionStorageSessionProvider = subscriptionStorageSessionProvider;
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                foreach (var messageType in messageTypes)
                {
                    if (session.QueryOver<Subscription>()
                        .Where(s => s.TypeName == messageType.TypeName && s.SubscriberEndpoint == address.ToString()).List()
                                .Any(s => new MessageType(s.TypeName, s.Version) == messageType))
                        continue;

                    session.Save(new Subscription
                                        {
                                            SubscriberEndpoint = address.ToString(),
                                            MessageType = messageType.TypeName + "," + messageType.Version,
                                            Version = messageType.Version.ToString(),
                                            TypeName = messageType.TypeName
                                        });
                }

                transaction.Complete();
            }
        }

        void ISubscriptionStorage.Unsubscribe(Address address, IEnumerable<MessageType> messageTypes)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var session = subscriptionStorageSessionProvider.OpenSession())
            {
                var subscriptions = session.QueryOver<Subscription>()
                    .Where(
                        s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()) &&
                                s.SubscriberEndpoint == address.ToString())
                    .List();

                foreach (var subscription in subscriptions.Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version))))
                    session.Delete(subscription);

                transaction.Complete();
            }
        }
        IEnumerable<Address> ISubscriptionStorage.GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {  
            using (new TransactionScope(TransactionScopeOption.Suppress))
            using (var session = subscriptionStorageSessionProvider.OpenStatelessSession())
                return session.QueryOver<Subscription>()
                    .Where(s => s.TypeName.IsIn(messageTypes.Select(mt => mt.TypeName).ToList()))
                    .List()
                    .Where(s => messageTypes.Contains(new MessageType(s.TypeName, s.Version)))
                    .Select(s => Address.Parse(s.SubscriberEndpoint))
                    .Distinct();
        }

        public void Init()
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }))
            using (var session = subscriptionStorageSessionProvider.OpenStatelessSession())
            {
                var v2XSubscriptions = session.QueryOver<Subscription>()
                    .Where(s => s.TypeName == null)
                    .List();
                if (v2XSubscriptions.Count == 0)
                    return;

                Logger.DebugFormat("Found {0} v2X subscriptions going to upgrade", v2XSubscriptions.Count);

                foreach (var v2XSubscription in v2XSubscriptions)
                {
                    var mt = new MessageType(v2XSubscription.MessageType);
                    v2XSubscription.Version = mt.Version.ToString();
                    v2XSubscription.TypeName = mt.TypeName;

                    session.Update(v2XSubscription);

                }
                transaction.Complete();
                Logger.InfoFormat("{0} v2X subscriptions upgraded", v2XSubscriptions.Count);
            }
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(ISubscriptionStorage));
    }
}