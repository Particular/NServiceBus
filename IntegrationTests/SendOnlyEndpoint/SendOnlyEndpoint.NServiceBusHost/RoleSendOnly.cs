using NServiceBus;
using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace SendOnlyEndpoint.NServiceBusHost
{
    public class RoleSendOnly : IConfigureRole<SendOnly>
    {
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            return null;
        }
    }
}