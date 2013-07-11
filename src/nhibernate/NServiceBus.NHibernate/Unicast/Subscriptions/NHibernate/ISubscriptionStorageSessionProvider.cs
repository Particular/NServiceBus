namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    using global::NHibernate;

    public interface ISubscriptionStorageSessionProvider
    {
        ISession OpenSession();
        IStatelessSession OpenStatelessSession();
    }
}