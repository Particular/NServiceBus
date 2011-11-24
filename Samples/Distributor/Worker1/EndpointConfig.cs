namespace Worker1
{
    using NServiceBus;
    using NServiceBus.Hosting;

    [EndpointName("Worker1")]
    internal class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
    }
}