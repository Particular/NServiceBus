namespace NServiceBus.Gateway.Channels.Http
{
    class SetDefaultResponder : IWantToRunBeforeConfigurationIsFinalized
    {
        public void Run(Configure config)
        {
            if (!config.Configurer.HasComponent<IHttpResponder>())
            {
                config.Configurer.ConfigureComponent<DefaultResponder>(DependencyLifecycle.InstancePerCall);
            }
        }
    }
}