using NServiceBus.Host;

namespace Server
{
    public class MessageEndpoint :  IConfigureThisEndpoint,
                                    ISpecify.ToUse.XmlSerialization,
                                    AsA_Server
    {
    }

}