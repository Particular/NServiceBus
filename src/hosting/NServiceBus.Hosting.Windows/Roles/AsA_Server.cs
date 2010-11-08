using NServiceBus.Hosting.Roles;

namespace NServiceBus
{
    /// <summary>
    /// Indicates this endpoint is a server.
    /// As such will be set up as a transactional endpoint using impersonation, not purging messages on startup.
    /// </summary>
    public interface AsA_Server:IRole {}
}