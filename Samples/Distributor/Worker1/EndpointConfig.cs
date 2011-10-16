namespace Worker1
{
    using NServiceBus;
    using NServiceBus.ObjectBuilder;

    internal class EndpointConfig : IConfigureThisEndpoint, AsA_Server
    {
    }
}