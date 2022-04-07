namespace NServiceBus.Persistence
{
    /// <summary>
    ///
    /// </summary>
    public interface ISynchronizedStorageSessionProvider
    {
        /// <summary>
        ///
        /// </summary>
        ISynchronizedStorageSession SynchronizedStorageSession { get; }
    }
}