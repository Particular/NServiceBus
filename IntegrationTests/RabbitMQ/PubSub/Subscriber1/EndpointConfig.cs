using NServiceBus;

namespace Subscriber1
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Server, UsingTransport<NServiceBus.RabbitMQ>
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