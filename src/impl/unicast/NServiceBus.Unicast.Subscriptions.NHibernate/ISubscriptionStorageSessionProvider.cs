using NHibernate;

namespace NServiceBus.Unicast.Subscriptions.NHibernate
{
    public interface ISubscriptionStorageSessionProvider
    {
        ISession OpenSession();
    }
}