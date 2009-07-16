using System;
using System.Collections.Generic;
using FluentNHibernate;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    /// <summary>
    /// Subscription storage using NHibernate for persistence 
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage,IDisposable
    {
        private ISession session;
        private readonly ISessionSource sessionSource;


        public SubscriptionStorage(ISessionSource sessionSource)
        {
            this.sessionSource = sessionSource;
        }

        private ISession Session
        {
            get
            {
                if (session == null)
                    session = sessionSource.CreateSession();

                return session;

            }
        }

        /// <summary>
        /// Adds the given subscription to the DB.
        /// Method check for existing subcriptions to prevent duplicates
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        public void Subscribe(string client, IList<string> messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                var subscription = new Subscription
                {
                    SubscriberEndpoint = client,
                    MessageType = messageType
                };

                if (Session.Get<Subscription>(subscription) == null)
                    Session.Save(subscription);

            }
        }

        /// <summary>
        /// Removes the specified subscriptions from DB
        /// </summary>
        /// <param name="client"></param>
        /// <param name="messageTypes"></param>
        public void Unsubscribe(string client, IList<string> messageTypes)
        {
            foreach (var messageType in messageTypes)
                Session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", client, messageType));
        }

        /// <summary>
        /// Listsa all subscribers for the specified message types
        /// </summary>
        /// <param name="messageTypes"></param>
        /// <returns></returns>
        public IList<string> GetSubscribersForMessage(IList<string> messageTypes)
        {
            var mt = new string[messageTypes.Count];
            messageTypes.CopyTo(mt, 0);

            return Session.CreateCriteria(typeof(Subscription))
                .Add(Restrictions.In("MessageType", mt))
                .SetProjection(Projections.Property("SubscriberEndpoint"))
                 .SetResultTransformer(new DistinctRootEntityResultTransformer())
                 .List<string>();
        }

        public void Init()
        {
            //No-op
        }

        public void Dispose()
        {
            if(session != null)
            {
                session.Flush();
                session.Close();
            }
                
        }
    }
}
