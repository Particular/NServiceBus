namespace NServiceBus
{
    using System.Transactions;
    using NServiceBus.Transports;

    /// <summary>
    /// Ambient transaction started by the transport receiver.
    /// </summary>
    public class AmbientTransaction : TransportTransaction
    {
        /// <summary>
        /// Ambient transaction.
        /// </summary>
        public Transaction Transaction { get; }

        /// <summary>
        /// Creates new ambient transport transaction.
        /// </summary>
        public AmbientTransaction(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}