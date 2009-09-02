using NServiceBus.Host;

namespace V2Subscriber
{
    public class V2SubscriberEndpoint : IConfigureThisEndpoint, 
                                        As.aServer,
                                        ISpecify.ToUse.XmlSerialization
    {

    }
}