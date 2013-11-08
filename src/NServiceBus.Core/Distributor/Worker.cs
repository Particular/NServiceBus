namespace NServiceBus.Distributor
{
    /// <summary>
    /// Worker details class, to be used with <see cref="IWorkerAvailabilityManager"/>.
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Worker"/> class.
        /// </summary>
        /// <param name="address">The <see cref="Address"/> of the worker that will accept the dispatched message.</param>
        /// <param name="sessionId">The current worker sessionId.</param>
        public Worker(Address address, string sessionId)
        {
            Address = address;
            SessionId = sessionId;
        }

        /// <summary>
        /// The <see cref="Address"/> of the worker that will accept the dispatched message.
        /// </summary>
        public Address Address { get; set; }
        
        /// <summary>
        /// The worker current sessionId.
        /// </summary>
        public string SessionId { get; set; }
    }
}