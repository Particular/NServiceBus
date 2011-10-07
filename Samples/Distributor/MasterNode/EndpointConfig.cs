namespace MasterNode
{
    using NServiceBus;

    public class EndpointConfig:IConfigureThisEndpoint,AsA_Server
    {
    }

    class MakeThisEndpointADistributor:IWantCustomInitialization
    {
        public void Init()
        {
            //todo: add a masternode profile handler that does this
            Configure.Instance.Distributor();
        }
    }

    
}