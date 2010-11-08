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

        /// <summary>
        /// Adds the given subscription to the DB.
        /// Method checks for existing subcriptions to prevent duplicates
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        public void Subscribe(string client, IEnumerable<string> messageTypes)
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
                session.Flush();
            }
        }

        /// <summary>
        /// Removes the specified subscriptions from DB
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        public void Unsubscribe(string client, IEnumerable<string> messageTypes)
        {

            using (var session = sessionSource.CreateSession())
            using (var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                    session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", client, messageType));

                transaction.Complete();
                session.Flush();
            }
        }

        /// <summary>
        /// Lists all subscribers for the specified message types
        /// </summary>
        /// <param name="messageTypes"></param>
        /// <returns></returns>
        public IEnumerable<string> GetSubscribersForMessage(IEnumerable<string> messageTypes)
        {
            var subscribers = new List<string>();

            using (var session = sessionSource.CreateSession())
            {
                subscribers.AddRange(from messageType in messageTypes
                                     from subscription in session.CreateCriteria(typeof (Subscription))
                                                            .Add(Restrictions.Eq("MessageType", messageType))
                                                            .List<Subscription>()
                                     select subscription.SubscriberEndpoint);
            }
            return subscribers;
        }

        public void Init()
        {
            //No-op
        }
    }
}
