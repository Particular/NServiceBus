namespace NServiceBus.Host.Tests
{
    public class ServerEndpointConfig : IConfigureThisEndpoint,
                                        As.aServer ,
                                        As.aPublisher  
    {
    }
}