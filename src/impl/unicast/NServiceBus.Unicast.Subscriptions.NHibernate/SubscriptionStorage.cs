using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using FluentNHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
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



        /// <summary>
        /// Adds the given subscription to the DB.
        /// Method checks for existing subcriptions to prevent duplicates
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            using (var session = sessionSource.CreateSession())
            using(var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                {
                    var subscription = new Subscription
                    {
                        SubscriberEndpoint = client,
                        MessageType = messageType
                    };

                    if (session.Get<Subscription>(subscription) == null)
                        session.Save(subscription);

                }


                transaction.Complete();
            }
        }

        /// <summary>
        /// Removes the specified subscriptions from DB
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        void ISubscriptionStorage.Unsubscribe(string client, IEnumerable<string> messageTypes)
        {

            using (var session = sessionSource.CreateSession())
            using (var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                    session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", client, messageType));

                transaction.Complete();
            }
        }

        /// <summary>
        /// Lists all subscribers for the specified message types
        /// </summary>
        /// <param name="messageTypes"></param>
        /// <returns></returns>
        IEnumerable<string> ISubscriptionStorage.GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            IEnumerable<string> result;

            using (var session = sessionSource.CreateSession())
            using (var transaction = new TransactionScope(TransactionScopeOption.RequiresNew,new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted}))
            {
                result = session.CreateCriteria(typeof(Subscription))
                                    .Add(Restrictions.In("MessageType", messageTypes.ToArray()))
                                    .SetProjection(Projections.Property("SubscriberEndpoint"))
                                    .SetResultTransformer(new DistinctRootEntityResultTransformer())
                                    .List<string>();

                transaction.Complete();
            }

            return result;
        }

        public void Init()
        {
            //No-op
        }
    }
}
