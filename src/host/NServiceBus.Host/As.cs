namespace NServiceBus
{
    /// <summary>
    /// Indicates this endpoint is a server.
    /// As such will be set up as a transactional endpoint using impersonation, not purging messages on startup.
    /// </summary>
    public interface AsA_Server {}

    /// <summary>
    /// Indicates this endpoint is a client.
    /// As such will be set up as a non-transactional endpoint with no impersonation and purging messages on startup.
    /// </summary>
    public interface AsA_Client {}

    /// <summary>
    /// Indicates this endpoint is a publisher.
    /// This is compatible with <see cref="AsA_Server"/> but not <see cref="AsA_Client"/>.
    /// </summary>
    public interface AsA_Publisher : AsA_Server {}
}
