namespace NServiceBus
{
    /// <summary>
    /// The different transaction levels that can be supported by a transport.
    /// </summary>
    public enum TransportTransactionMode
    {
        /// <summary>
        /// No transactions used. This means that received messages will not be roll the message back to the queue if a failure
        /// occurs.
        /// This means that the message is lost.
        /// </summary>
        None = 0,

        /// <summary>
        /// The receive operation will be transactional and the message will be rolled back to the queue in case of failure.
        /// Outgoing queueing operations will not be enlisted in the ongoing receive transaction and therefor NOT roll back should
        /// a failure occur.
        /// </summary>
        ReceiveOnly = 1,

        /// <summary>
        /// In this mode all outgoing operations will be atomic with the current receive operations.
        /// </summary>
        SendsAtomicWithReceive = 2,

        /// <summary>
        /// The transport enlists its receive operation in a transaction scope allowing other resource managers to participate.
        /// </summary>
        TransactionScope = 3
    }
}