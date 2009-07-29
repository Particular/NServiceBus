namespace NServiceBus.Host
{
    /// <summary>
    /// Container class for interface specifications.
    /// Implement the contained interfaces on the class which implements <see cref="IConfigureThisEndpoint"/>.
    /// </summary>
    public class As
    {
        /// <summary>
        /// Indicates this endpoint is a server.
        /// As such will be set up as a transactional endpoint using impersonation, not purging messages on startup.
        /// </summary>
        public interface aServer {}

        /// <summary>
        /// Indicates this endpoint is a client.
        /// As such will be set up as a non-transactional endpoint with no impersonation and purging messages on startup.
        /// </summary>
        public interface aClient {}

        /// <summary>
        /// Indicates this endpoint is a publisher.
        /// This is compatible with <see cref="aServer"/> but not <see cref="aClient"/>.
        /// </summary>
        public interface aPublisher : aServer {}

        /// <summary>
        /// Indicates that this endpoint is used to host sagas
        /// </summary>
        public interface aSagaHost :aServer{}
    }
}
