namespace MyPublisher
{
    using NServiceBus;
    using NServiceBus.Unicast.Queuing.ActiveMQ;

    class EndpointConfig :  IConfigureThisEndpoint, AsA_Publisher,IWantCustomInitialization
    {
        public void Init()
        {
            var config = Configure.With()
                //this overrides the NServiceBus default convention of IEvent
                .DefaultBuilder()
                .ActiveMqTransport("A", "activemq:tcp://localhost:61616")
                .XmlSerializer(dontWrapSingleMessages: true)
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));
        }
    }
}
