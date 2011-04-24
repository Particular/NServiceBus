namespace NServiceBus.Gateway.Config
{
    using NServiceBus.Config;

    public class MasterNodeBootstrapper : INeedInitialization
    {
        void INeedInitialization.Init()
        {
            bool isMasterNode = false;

            //todo -  we need a way to know if we're the master node without having DI
            if (isMasterNode)
                Configure.Instance.Gateway();
        }
    }
}