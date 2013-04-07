namespace NServiceBus.Sagas
{
    using Config;
    using Saga;

    public class FinalizeSagaConfiguration : IFinalizeConfiguration
    {
        public void FinalizeConfiguration()
        {
            if(Feature.IsEnabled<Features.Sagas>())
                Infrastructure.Enable<ISagaPersister>();
        }
    }
}