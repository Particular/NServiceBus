using NServiceBus;

namespace OrderService
{
    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Worker, UsingTransport<WindowsAzureServiceBus>
    {
        
    }
}