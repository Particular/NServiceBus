using NServiceBus;

namespace MyPublisher
{
    class EndpointConfig :  IConfigureThisEndpoint, AsA_Publisher,IWantCustomInitialization
    {
        public void Init()
        {
            Configure.With()
                .DefaultBuilder()
                //this overrides the NServiceBus default convention of IEvent
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.StartsWith("MyMessages"));}
    }
}
