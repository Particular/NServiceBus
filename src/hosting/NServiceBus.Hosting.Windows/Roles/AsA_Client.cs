namespace NServiceBus
{
    using Hosting.Roles;

    /// <summary>
    /// Indicates this endpoint is a client.
    /// As such will be set up as a non-transactional endpoint with no impersonation and purging messages on startup.
    /// </summary>
    public interface AsA_Client:IRole {}
}