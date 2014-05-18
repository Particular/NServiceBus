namespace NServiceBus.Unicast.Config
{
    using Transports;

    /// <summary>
    /// Default to MSMQ transport if no other transport has been configured. This can be removed when we introduce the modules concept
    /// </summary>
    class DefaultTransportForHost : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            if (config.Configurer.HasComponent<ISendMessages>())
            {
                return;
            }

            if(config.Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transport.SelectedTransport") != null)
            {
                return;
            }

            config.UseTransport<Msmq>();
        }
    }
}