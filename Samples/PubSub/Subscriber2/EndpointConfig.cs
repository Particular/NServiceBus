using NServiceBus.Host;
using NServiceBus;

namespace Subscriber2
{
    public class EndpointConfig : IConfigureThisEndpoint, ISpecifyProfile<MyProfile> {}

    public class MyProfile : Lite {}

    public class NsbConfig : IConfigureTheBusForProfile<MyProfile>
    {
        public void Configure(IConfigureThisEndpoint specifier)
        {
            NServiceBus.Configure.With()
                .UnityBuilder() // just to show we can mix and match containers
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                .UnicastBus()
                    .DoNotAutoSubscribe(); //managed by the class Subscriber2Endpoint
        }
    }
}
