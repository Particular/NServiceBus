namespace NServiceBus.Distributor.Config
{
    using Settings;

    class DefaultMasterNodeAddress : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run()
        {
            SettingsHolder.SetDefault("MasterNode.Address", Address.Parse(Configure.EndpointName));
        }
    }
}