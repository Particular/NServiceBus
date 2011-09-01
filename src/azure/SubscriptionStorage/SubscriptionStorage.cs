using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NHibernate.Criterion;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage
{
    /// <summary>
    /// Subscription storage using NHibernate for persistence 
    /// </summary>
    public class SubscriptionStorage : ISubscriptionStorage
    {
      private readonly ISubscriptionStorageSessionProvider sessionSource;

      public SubscriptionStorage(ISubscriptionStorageSessionProvider sessionSource)
        {
            this.sessionSource = sessionSource;
        }

        void ISubscriptionStorage.Subscribe(string client, IEnumerable<string> messageTypes)
        {
            ((ISubscriptionStorage)this).Subscribe(Address.Parse(client), messageTypes);
        }

        void ISubscriptionStorage.Subscribe(Address address, IEnumerable<string> messageTypes)
        {
            using (var session = sessionSource.OpenSession())
            using(var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                {
                    var subscription = new Subscription
                    {
                        SubscriberEndpoint = EncodeTo64(address.ToString()),
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
            var encodedAddress = EncodeTo64(address.ToString());
            using (var session = sessionSource.OpenSession())
            using (var transaction = new TransactionScope())
            {
                foreach (var messageType in messageTypes)
                    session.Delete(string.Format("from Subscription where SubscriberEndpoint = '{0}' AND MessageType = '{1}'", encodedAddress, messageType));

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

            using (var session = sessionSource.OpenSession())
            {
                subscribers.AddRange(from messageType in messageTypes
                                     from subscription in session.CreateCriteria(typeof (Subscription))
                                                            .Add(Restrictions.Eq("MessageType", messageType))
                                                            .List<Subscription>()
                                     select Address.Parse(DecodeFrom64(subscription.SubscriberEndpoint)));
            }

            return subscribers;
        }

        public void Init()
        {
            //No-op
        }

        static public string EncodeTo64(string toEncode)
        {
            var toEncodeAsBytes= System.Text.Encoding.ASCII.GetBytes(toEncode);
            var returnValue = System.Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        static public string DecodeFrom64(string encodedData)
        {
            var encodedDataAsBytes = System.Convert.FromBase64String(encodedData);
            var returnValue = System.Text.Encoding.ASCII.GetString(encodedDataAsBytes);
            return returnValue;
        }
    }
}
