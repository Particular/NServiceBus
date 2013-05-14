namespace NServiceBus.Config
{
    using Saga;
    using Timeout.Core;

    public class AzureServiceBusPersistence
    {
        public static void UseAsDefault()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.AzureSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseAzureTimeoutPersister());
        }
    }
}