namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage
{
    using global::NHibernate;

    public interface ISubscriptionStorageSessionProvider
    {
        ISession OpenSession();
        IStatelessSession OpenStatelessSession();
    }
}