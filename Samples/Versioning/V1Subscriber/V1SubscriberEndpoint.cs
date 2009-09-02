using NServiceBus;
using NServiceBus.Host;

namespace V1Subscriber
{
    public class V1SubscriberEndpoint : IConfigureThisEndpoint, 
                                        As.aServer,
                                        ISpecify.ToUse.XmlSerialization
    {
        
    }
}