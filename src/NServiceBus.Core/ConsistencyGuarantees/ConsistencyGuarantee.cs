namespace NServiceBus.ConsistencyGuarantees
{
    /// <summary>
    /// Consistency guarantees that can be requested by the user.
    /// </summary>
    public enum ConsistencyGuarantee
    {
        /// <summary>
        /// Message should be handled at most once. This is the lowest guarantee and allows the transport
        /// to avoid any kind of transactions when performing the receive operation.
        /// </summary>
        AtMostOnce = 1,

        /// <summary>
        /// Message should be delivered at least once. It's ok to not be atomic with the ongoing receive transaction should one be present.
        /// </summary>
        AtLeastOnce = 2,

        /// <summary>
        /// Message should be processed exactly once from a business logic perspective.
        /// This can be supported in two ways. 
        /// * Both transport and storage supports distributed transactions.
        /// * Endpoint configured to use the Outbox feature. 
        /// </summary>
        ExactlyOnce = 3
    }
}