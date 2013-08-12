namespace NServiceBus.Persistence.InMemory
{
    using Config;
    using Gateway.Deduplication;
    using Gateway.Persistence;
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class InMemoryPersistence
    {
        public static void UseAsDefault()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.InMemorySagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseInMemoryTimeoutPersister());
            InfrastructureServices.SetDefaultFor<IPersistMessages>(() => Configure.Instance.UseInMemoryGatewayPersister());
            InfrastructureServices.SetDefaultFor<IDeduplicateMessages>(() => Configure.Instance.UseInMemoryGatewayDeduplication());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.InMemorySubscriptionStorage());
        }
    }
}