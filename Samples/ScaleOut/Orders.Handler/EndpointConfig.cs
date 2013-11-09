using NServiceBus;

namespace Orders.Handler
{
    internal class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher
    {
    }

    internal class ConfiguringTheDistributorWithTheFluentApi : INeedInitialization
    {
        public void Init()
        {
            //uncomment one of the following lines if you want to use the fluent api instead. Remember to 
            // remove the "MSMQMaster" profile from the command line Properties->Debug
            //Configure.Instance.AsMasterNode();

            //or if you want to run the distributor only and no worker
            //Configure.Instance.RunMSMQDistributor(false);

            //or if you want to be a worker
            //Configure.Instance.EnlistWithMSMQDistributor();
        }
    }
}