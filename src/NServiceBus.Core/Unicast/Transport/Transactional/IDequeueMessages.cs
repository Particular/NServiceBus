namespace NServiceBus.Unicast.Transport.Transactional
{
    using System;

    /// <summary>
    /// Interface to implement when developing custom dequeing strategies.
    /// </summary>
    public interface IDequeueMessages
    {
        /// <summary>
        /// Initialises the <see cref="IDequeueMessages"/>.
        /// </summary>
        /// <param name="address">The address to listen on.</param>
        /// <param name="settings">The <see cref="TransactionSettings"/> to be used by <see cref="IDequeueMessages"/>.</param>
        void Init(Address address, TransactionSettings transactionSettings);
        
        /// <summary>
        /// Starts the dequeuing of message using the specified <paramref name="maxDegreeOfParallelism"/>.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The max degree of parallelism supported.</param>
        void Start(int maxDegreeOfParallelism);
        
        /// <summary>
        /// updates the max degree of parallelism supported.
        /// </summary>
        /// <param name="value">The new max degree of parallelism supported.</param>
        void ChangeMaxDegreeOfParallelism(int value);

        /// <summary>
        /// Stops the dequeuing of messages.
        /// </summary>
        void Stop();

        /// <summary>
        /// Fires when a message has been dequeued.
        /// </summary>
        event EventHandler<TransportMessageAvailableEventArgs> MessageDequeued; 
    }
}