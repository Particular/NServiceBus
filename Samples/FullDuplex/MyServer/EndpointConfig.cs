using NServiceBus;

namespace MyServer
{
    [EndpointSLA("00:00:30")]
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server {
    }
}