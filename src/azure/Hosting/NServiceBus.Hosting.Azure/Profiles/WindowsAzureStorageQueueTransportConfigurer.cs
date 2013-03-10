namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Configures windows azure storage queues as the underlying transport.
    /// </summary>
    public class WindowsAzureStorageQueueTransportConfigurer : IConfigureTransport<NServiceBus.WindowsAzureStorage>
    {
        public void Configure(Configure config)
        {
            config.AzureMessageQueue();
        }
    }
}