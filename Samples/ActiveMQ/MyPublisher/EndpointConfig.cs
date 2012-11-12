using NServiceBus;
using NServiceBus.Unicast.Queuing.ActiveMQ;

namespace MyPublisher
{
    using Apache.NMS;

    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Unicast.Queuing;

    class EndpointConfig :  IConfigureThisEndpoint, AsA_Publisher,IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefaultBuilder()
                .ActiveMqTransport("A", "activemq:tcp://localhost:61616")
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));
        }
    }
}
