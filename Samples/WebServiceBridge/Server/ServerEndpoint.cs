using NServiceBus.Host;

namespace Server
{
    public class ServerEndpoint :   IConfigureThisEndpoint,
                                    As.aServer
    {
    }
}