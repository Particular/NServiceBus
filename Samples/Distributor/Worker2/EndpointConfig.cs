namespace Worker2
{
    using NServiceBus;
    using NServiceBus.Hosting;

    [EndpointName("Worker2")]
    internal class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
    }
}