namespace NServiceBus
{
    using Transports;

    /// <summary>
    /// Transport definition for WindowsAzureServiceBus    
    /// </summary>
    public class WindowsAzureServiceBus : TransportDefinition
    {
        public WindowsAzureServiceBus()
        {
            HasNativePubSubSupport = true;
        }
    }
}