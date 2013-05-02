namespace NServiceBus.Config
{
    using Features;
    using Saga;
    using Timeout.Core;
    using Unicast.Config;
    using Unicast.Subscriptions;

    public class AzureServiceBusPersistence
    {
        public static void UseAsDefault()
        {
            new ApplyDefaultAutoSubscriptionStrategy().Run();
            InfrastructureServices.Enable<IAutoSubscriptionStrategy>();

            Feature.Enable<TimeoutManager>();


            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());
        }
    }
}