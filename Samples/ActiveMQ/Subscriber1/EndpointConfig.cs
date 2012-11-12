using NServiceBus;

namespace Subscriber1
{
    using Apache.NMS;

    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Queuing.ActiveMQ;

    class EndpointConfig : IConfigureThisEndpoint, AsA_Server,IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefaultBuilder()
                .ActiveMqTransport("B", "activemq:tcp://localhost:61616")
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));

            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}