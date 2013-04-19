namespace NServiceBus
{
    using Config;
    using Hosting.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Serialization;
    using Transports;

    /// <summary>
    /// Configures windows azure storage queues as the underlying transport.
    /// </summary>
    public class WindowsAzureStorageQueueTransportConfigurer : IConfigureTransport<NServiceBus.WindowsAzureStorage>, IWantTheEndpointConfig
    {
        public void Configure(Configure config)
        {
            if (RoleEnvironment.IsAvailable && !IsHostedIn.ChildHostProcess())
            {
                config.AzureConfigurationSource();
            }

            if (!config.Configurer.HasComponent<IMessageSerializer>())
            {
                config.JsonSerializer();
            }

            config
                .AzureMessageQueue();

        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}