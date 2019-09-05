namespace NServiceBus
{
    using System.Threading.Tasks;

    class ConfiguredInternalContainerEndpoint
    {
        public ConfiguredInternalContainerEndpoint(ConfiguredEndpoint configuredEndpoint)
        {
            this.configuredEndpoint = configuredEndpoint;
        }
        public Task<IStartableEndpoint> Initialize()
        {
            return configuredEndpoint.Initialize();
        }

        ConfiguredEndpoint configuredEndpoint;
    }
}