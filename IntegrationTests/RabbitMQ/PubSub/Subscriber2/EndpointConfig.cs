using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, UsingTransport<NServiceBus.RabbitMQ>
    {
      
    }

    class TurnOffAutoSubscribe:INeedInitialization
    {
        public void Init()
        {
            Configure.Instance.UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint
        }
    }

    class MyClass : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            Configure.Instance.DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));
        }
    }
}
