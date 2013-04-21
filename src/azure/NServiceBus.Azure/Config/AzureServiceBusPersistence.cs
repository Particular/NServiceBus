namespace NServiceBus.Config
{
    using Features;
    using Saga;
    using Timeout.Core;

    public class AzureServiceBusPersistence
    {
        public static void UseAsDefault()
        {
            //Feature.Enable<MessageDrivenSubscriptions>();
            Feature.Enable<TimeoutManager>();

            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());
        }
    }
}