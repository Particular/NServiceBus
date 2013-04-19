namespace NServiceBus.Config
{
    using Gateway.Persistence;
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class AzureServiceBusPersistence
    {
        public static void UseAsDefault()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.InMemorySagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseInMemoryTimeoutPersister());
            InfrastructureServices.SetDefaultFor<IPersistMessages>(() => Configure.Instance.UseInMemoryGatewayPersister());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.InMemorySubscriptionStorage());
        }
    }
}