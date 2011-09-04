namespace NServiceBus.Unicast.Queuing
{
    /// <summary>
    /// Indicates that the queue can be purged on startup
    /// </summary>
    public interface ICanPurgeOnStartup
    {
        /// <summary>
        /// Sets whether or not the transport should purge the input
        /// queue when it is started.
        /// </summary>
        bool PurgeOnStartup { get; set; }
    }
}