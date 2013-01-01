using NServiceBus;

namespace MyPublisher
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher, UsingTransport<NServiceBus.RabbitMQ>
    {
    }

    class MyClass : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));
        }
    }

}
