using NServiceBus;

namespace Orders.Handler
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher { }
    
    class ConfiguringTheDistributorWithTheFluentApi : INeedInitialization
    {
        public void Init()
        {
            //uncomment one of the following lines if you want to use the fluent api instead. Remember to 
            // remove the "Master" profile from the command line Properties->Debug
            //Configure.Instance.RunDistributor();

            //or if you want to run the distributor only and no worker
            //Configure.Instance.RunDistributorWithNoWorkerOnItsEndpoint();

            //or if you want to be a worker
            //Configure.Instance.EnlistWithDistributor();
        }
    }
}
