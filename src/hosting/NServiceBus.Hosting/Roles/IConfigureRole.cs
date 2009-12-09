using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Roles
{

    public interface IConfigureRole
    {
        ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier);
    }

    public interface IConfigureRole<T> : IConfigureRole where T : IRole
    {
    }
}