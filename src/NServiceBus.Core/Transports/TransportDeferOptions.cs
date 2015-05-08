namespace NServiceBus.Transports
{
    using System;

    /// <summary>
    /// Contains details on how the message should be deferred
    /// </summary>
    public class TransportDeferOptions
    {
        /// <summary>
        /// Creates the options with a destination and a delay
        /// </summary>
        /// <param name="destination">The native address where to sent this message</param>
        /// <param name="deliverAt">Instructs the transport to deliver the message at the given time</param>
        /// <param name="nonDurable">Message durability, default is `true`</param>
        /// <param name="enlistInReceiveTransaction">Tells the transport to enlist the send in its native transaction if supported</param>
        /// <param name="delayDeliveryFor">Tells the transport to delay the delivery with the given timespan</param>
        public TransportDeferOptions(string destination, TimeSpan? delayDeliveryFor=null, DateTime? deliverAt=null, bool nonDurable = false, bool enlistInReceiveTransaction = true)
        {
            if (delayDeliveryFor.HasValue && deliverAt.HasValue)
            {
                throw new ArgumentException("Both deliverAt and delayDeliverFor can't be specified at the same time");
            }
            if (!delayDeliveryFor.HasValue && !deliverAt.HasValue)
            {
                throw new ArgumentException("Either deliverAt or delayDeliverFor needs to be specified");
            }

            Destination = destination;
            DelayDeliveryFor = delayDeliveryFor;
            DeliverAt = deliverAt;
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
        /// The time when the message should be delivered to the destination.
        /// </summary>
        public DateTime? DeliverAt { get; private set; }

        /// <summary>
        /// How long to delay delivery of the message.
        /// </summary>
        public TimeSpan? DelayDeliveryFor { get; private set; }

    }
}