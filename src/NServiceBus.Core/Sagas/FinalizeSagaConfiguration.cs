namespace NServiceBus.Sagas
{
    using Config;
    using Features;
    using Saga;

    public class FinalizeSagaConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if (!Feature.IsEnabled<Sagas>())
                return;

            InfrastructureServices.Enable<ISagaPersister>();
        }
    }
}