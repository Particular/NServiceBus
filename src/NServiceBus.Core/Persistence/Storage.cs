namespace NServiceBus.Persistence
{
    /// <summary>
    /// The storage needs of NServiceBus
    /// </summary>
    public enum Storage
    {
        /// <summary>
        /// Storage for timeouts
        /// </summary>
        Timeouts = 1,
        /// <summary>
        /// Storage for subscriptions
        /// </summary>
        Subscriptions = 2,
        /// <summary>
        /// Storage for sagas
        /// </summary>
        Sagas = 3,
        /// <summary>
        /// Storage for gateway deduplication
        /// </summary>
        GatewayDeduplication = 4,
        /// <summary>
        /// Storage for the outbox
        /// </summary>
        Outbox = 5,
    }
}