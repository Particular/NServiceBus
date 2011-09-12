using NServiceBus.Hosting.Roles;

namespace NServiceBus
{
    /// <summary>
    /// Indicates this endpoint is a host that merely loads other processes.
    /// </summary>
    public interface AsA_Host : IRole { }
}