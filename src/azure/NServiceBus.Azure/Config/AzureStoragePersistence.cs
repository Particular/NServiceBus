namespace NServiceBus.Config
{
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class AzureStoragePersistence
    {
        public static void UseAsDefault()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.AzureSubcriptionStorage());
        }
    }
}