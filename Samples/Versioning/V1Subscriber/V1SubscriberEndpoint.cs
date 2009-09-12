using NServiceBus;
using NServiceBus.Host;

namespace V1Subscriber
{
    public class V1SubscriberEndpoint : IConfigureThisEndpoint, 
                                        AsA_Server,
                                        ISpecify.ToUse.XmlSerialization
    {
        
    }
}