using NServiceBus.Host;

namespace V2Subscriber
{
    public class V2SubscriberEndpoint : IConfigureThisEndpoint, 
                                        AsA_Server,
                                        ISpecify.ToUse.XmlSerialization
    {

    }
}