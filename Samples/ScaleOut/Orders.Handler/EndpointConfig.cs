using NServiceBus;

namespace Orders.Handler
{
    using NServiceBus.Config;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, IWantCustomLogging
    {
        public void Init()
        {
            SetLoggingLibrary.Log4Net(log4net.Config.XmlConfigurator.Configure);

        }
    }

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
