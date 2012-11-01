using NHibernate;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage
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
            return this.sessionFactory.OpenSession();
        }

        public IStatelessSession OpenStatelessSession()
        {
            return this.sessionFactory.OpenStatelessSession();
        }
    }
}