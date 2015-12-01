namespace NServiceBus.Transports
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a transaction used to receive the message from the queueing infrastructure.
    /// </summary>
    public class TransportTransaction
    {
        /// <summary>
        /// Used for passing information about transport-level transaction to enable sharing it between transport and persistance.
        /// </summary>
        public IDictionary<string, object> Data { get; set; }
        
        /// <summary>
        /// Creates new transport transaction.
        /// </summary>
        public TransportTransaction()
        {
            Data = new Dictionary<string, object>();
        }
    }
}