using NServiceBus.Host;

namespace Server
{
    public class MessageEndpoint :  IConfigureThisEndpoint,
                                    ISpecify.ToUse.XmlSerialization,
                                    As.aServer
    {
    }

}