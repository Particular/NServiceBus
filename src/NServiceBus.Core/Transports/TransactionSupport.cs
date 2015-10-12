namespace NServiceBus.Transports
{
    /// <summary>
    /// The different transaction levels that can be supported by a transport.
    /// </summary>
    public enum TransactionSupport
    {
        /// <summary>
        /// No transactions supported. Failures will not roll the message back to the queue.
        /// </summary>
        None = 0,

        /// <summary>
        /// Supports transactions for a single queue. Messages failing can be rolled back the the queue.
        /// Outgoing queueing operations can not enlist in the receive transaction.
        /// </summary>
        SingleQueue = 1,

        /// <summary>
        /// Has support for transactions spanning multiple queues. Outgoing operations can enlist in the receive transaction.
        /// </summary>
        MultiQueue = 2,

        /// <summary>
        /// The transport supports and can participate in distributed transactions.
        /// </summary>
        Distributed = 3
    }
}