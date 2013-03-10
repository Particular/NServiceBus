namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Configures windows azure servicebus as the underlying transport.
    /// </summary>
    public class WindowsAzureServiceBusQueueTransportConfigurer : IConfigureTransport<NServiceBus.WindowsAzureServiceBus>
    {
        public void Configure(Configure config)
        {
            config.AzureServiceBusMessageQueue();
        }
    }
}