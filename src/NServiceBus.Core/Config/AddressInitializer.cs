namespace NServiceBus.Config
{
    class AddressInitializer : IWantToRunBeforeConfiguration
    {
        public void Init(Configure configure)
        {
            if (Address.Local == null)
            {
                Address.InitializeLocalAddress(configure.Settings.EndpointName());
            }
        }
    }
}