namespace NServiceBus.Config
{
    using Features;
    using Gateway.Persistence;
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    public class AzureStoragePersistence
    {
        public static void UseAsDefault()
        {
            Feature.Enable<MessageDrivenSubscriptions>();
            Feature.Enable<MessageDrivenPublisher>();
            Feature.Enable<TimeoutManager>();

            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.AzureSubcriptionStorage());
        }
    }
}