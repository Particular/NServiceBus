using NServiceBus.Host;

namespace Server
{
    public class MessageEndpoint :  IConfigureThisEndpoint,
                                    ISpecify.ToUseXmlSerialization,
                                    As.aServer
    {
    }

}