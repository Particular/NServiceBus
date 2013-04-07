namespace NServiceBus.Persistence.InMemory
{
    using Config;
    using Gateway.Persistence;
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions;

    public class InMemoryPersistence
    {
        public static void UseAsDefault()
        {
            Infrastructure.SetDefaultFor<ISagaPersister>(() => Configure.Instance.InMemorySagaPersister());
            Infrastructure.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseInMemoryTimeoutPersister());
            Infrastructure.SetDefaultFor<IPersistMessages>(() => Configure.Instance.UseInMemoryGatewayPersister());
            Infrastructure.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.InMemorySubscriptionStorage());
        }
    }
}