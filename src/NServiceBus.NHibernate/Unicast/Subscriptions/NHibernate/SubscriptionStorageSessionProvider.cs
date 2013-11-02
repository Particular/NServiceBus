namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using global::NHibernate;

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

        public IStatelessSession OpenStatelessSession()
        {
            return sessionFactory.OpenStatelessSession();
        }
    }
}