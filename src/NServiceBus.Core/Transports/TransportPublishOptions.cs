namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Contains details on how the message should be published
    /// </summary>
    public class TransportPublishOptions
    {
        /// <summary>
        /// Creates the send options with the given address
        /// </summary>
        /// <param name="eventType">The type of event being published</param>
        /// <param name="timeToBeReceived">Optional TTBR for the message</param>
        /// <param name="nonDurable">Message durability, default is `true`</param>
        /// <param name="enlistInReceiveTransaction">Tells the transport to enlist the send in its native transaction if supported</param>
        public TransportPublishOptions(Type eventType, TimeSpan? timeToBeReceived = null, bool nonDurable = false, bool enlistInReceiveTransaction = true)
        {
            EventType = eventType;
            TimeToBeReceived = timeToBeReceived;
            NonDurable = nonDurable;
            EnlistInReceiveTransaction = enlistInReceiveTransaction;
        }

        /// <summary>
        /// The type of event being published
        /// </summary>
        public Type EventType { get; private set; }

        /// <summary>
        /// Tells if the send operation should be enlisted in the current (if any) receive transaction
        /// </summary>
        public bool EnlistInReceiveTransaction { get; private set; }

        /// <summary>
        /// Tells if the message should be sent as a non durable message
        /// </summary>
        public bool NonDurable { get; private set; }

        /// <summary>
        /// Optional TTBR for this message
        /// </summary>
        public TimeSpan? TimeToBeReceived { get; private set; }
    }
}