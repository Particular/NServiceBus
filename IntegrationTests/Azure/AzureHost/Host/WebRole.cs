using NServiceBus;
using NServiceBus.Hosting.Azure;

namespace Host
{
    public class WebRole : RoleEntryPoint
    {

    }

    public class EndpointConfiguration : IConfigureThisEndpoint, AsA_Host
    {
    }
}
