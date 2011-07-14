using NServiceBus;
using NServiceBus.Hosting.Azure.Roles.Handlers;

namespace OrderService
{
    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Server, 
                                         ICommunicateThroughAppFabricQueues { }
}