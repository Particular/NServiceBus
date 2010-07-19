using NHibernate;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    public class SubscriptionStorageSessionProvider:ISubscriptionStorageSessionProvider
    {
        readonly ISessionFactory sessionFactory;

        public SubscriptionStorageSessionProvider(ISessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory;
        }

        public ISession OpenSession()
        {
            return sessionFactory.OpenSession();
        }
    }
}