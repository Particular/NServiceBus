namespace NServiceBus.Distributor.Config
{
    class DefaultMasterNodeAddress : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            config.Settings.SetDefault("MasterNode.Address", Address.Parse(config.Settings.EndpointName()));
        }
    }
}