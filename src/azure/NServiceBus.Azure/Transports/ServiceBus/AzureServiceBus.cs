namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Transport definition for WindowsAzureServiceBus    
    /// </summary>
    public class AzureServiceBus : TransportDefinition
    {
        public AzureServiceBus()
        {
            HasNativePubSubSupport = true;
            HasSupportForCentralizedPubSub = true;
        }
    }
}