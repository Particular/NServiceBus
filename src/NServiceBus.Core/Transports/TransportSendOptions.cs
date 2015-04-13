namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Contains details on how the message should be sent
    /// </summary>
    public class TransportSendOptions
    {
        /// <summary>
        /// Creates the send options with the given address
        /// </summary>
        /// <param name="destination">The native address where to sent this message</param>
        /// <param name="timeToBeReceived">Optional TTBR for the message</param>
        /// <param name="nonDurable">Message durability, default is `true`</param>
        /// <param name="enlistInReceiveTransaction">Tells the transport to enlist the send in its native transaction if supported</param>
        public TransportSendOptions(string destination, TimeSpan? timeToBeReceived = null, bool nonDurable = false, bool enlistInReceiveTransaction = true)
        {
            Destination = destination;
            TimeToBeReceived = timeToBeReceived;
            NonDurable = nonDurable;
            EnlistInReceiveTransaction = enlistInReceiveTransaction;
        }

        /// <summary>
        /// The address where this message should be sent to
        /// </summary>
        public string Destination { get; private set; }

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