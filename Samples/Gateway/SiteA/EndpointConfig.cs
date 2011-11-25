using NServiceBus;

namespace SiteA
{
    //endpoint is stated in the Lite profile which turns the gateway on by default
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
    }
}
