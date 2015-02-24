namespace NServiceBus.Distributor.Config
{
    class DefaultMasterNodeAddress : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            config.Settings.SetDefault("MasterNode.Address", config.Settings.EndpointName());
        }
    }
}