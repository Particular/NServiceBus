namespace NServiceBus
{
    using Config;
    using Hosting.Azure;
    using Microsoft.WindowsAzure.ServiceRuntime;
    using Serialization;
    using Transports;

    /// <summary>
    /// Configures windows azure servicebus as the underlying transport.
    /// </summary>
    public class WindowsAzureServiceBusQueueTransportConfigurer : IConfigureTransport<NServiceBus.WindowsAzureServiceBus>
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

            config.AzureServiceBusMessageQueue();
        }
    }
}