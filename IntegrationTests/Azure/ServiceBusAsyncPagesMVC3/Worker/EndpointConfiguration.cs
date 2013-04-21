using NServiceBus;

namespace Worker
{
    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, UsingTransport<WindowsAzureServiceBus>
    {
        
    }
}