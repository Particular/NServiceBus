namespace NServiceBus.Transports
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Allows the transport to pass relevant information into the bus session whenever a new bus session is created.
    /// </summary>
    public class SessionContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="transportTransaction">Transaction (along with connection if applicable) used to send messages.</param>
        /// <param name="context">Context provided by the transport.</param>
        public SessionContext(TransportTransaction transportTransaction, ContextBag context)
        {
            Context = context;
            TransportTransaction = transportTransaction;
        }

        /// <summary>
        /// Transaction (along with connection if applicable) used to send messages.
        /// </summary>
        public TransportTransaction TransportTransaction { get; }

        /// <summary>
        /// Context provided by the transport.
        /// </summary>
        public ContextBag Context { get; }
    }
}