namespace NServiceBus.Features
{
    using Config;
    using Saga;

    public class Sagas : IFeature
    {
        public void Initalize()
        {
            //todo: Move the saga init code over here


            InfrastructureServices.Enable<ISagaPersister>();
        }
    }
}