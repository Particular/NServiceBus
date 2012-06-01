using NHibernate;

namespace NServiceBus.Unicast.Subscriptions.Azure.TableStorage
{
    public interface ISubscriptionStorageSessionProvider
    {
        ISession OpenSession();
        IStatelessSession OpenStatelessSession();
    }
}