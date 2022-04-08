namespace NServiceBus.Persistence
{
    /// <summary>
    /// Provides access to the <see cref="ISynchronizedStorageSession"/>.
    /// </summary>
    public interface ISynchronizedStorageSessionProvider
    {
        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        ISynchronizedStorageSession SynchronizedStorageSession { get; }
    }
}