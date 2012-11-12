using NServiceBus;

namespace Subscriber2
{
    using Apache.NMS;

    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Unicast.Queuing;
    using NServiceBus.Unicast.Queuing.ActiveMQ;

    public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefiningEventsAs(t=> t.Namespace != null && t.Namespace.StartsWith("MyMessages"))
                .CastleWindsorBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .ActiveMqTransport("C", "activemq:tcp://localhost:61616")
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint

            Configure.Instance.DisableSecondLevelRetries();
        }
    }
}
