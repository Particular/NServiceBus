namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Controls how a message will be sent by the transport
    /// </summary>
    public partial class SendOptions : DeliveryOptions
    {
        readonly TimeSpan? delayDeliveryFor;
        readonly string destination;
        readonly DateTime? deliverAt;

        /// <summary>
        /// Creates an instance of <see cref="SendOptions"/>.
        /// </summary>
        /// <param name="destination">Address where to send this message.</param>
        /// <param name="deliverAt">The time when the message should be delivered to the destination.</param>
        /// <param name="delayDeliveryFor">How long to delay delivery of the message.</param>
        public SendOptions(string destination, DateTime? deliverAt = null, TimeSpan? delayDeliveryFor = null)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            this.destination = destination;

            if (deliverAt != null && delayDeliveryFor != null)
            {
                throw new ArgumentException("Ensure you either set `deliverAt` or `delayDeliveryFor`, but not both.");
            }

            this.deliverAt = deliverAt;

            Guard.AgainstNegative(delayDeliveryFor, "delayDeliveryFor");
            this.delayDeliveryFor = delayDeliveryFor;
        }

        /// <summary>
        /// The time when the message should be delivered to the destination.
        /// </summary>
        public DateTime? DeliverAt
        {
            get { return deliverAt; }
        }

        /// <summary>
        /// How long to delay delivery of the message.
        /// </summary>
        public TimeSpan? DelayDeliveryFor
        {
            get { return delayDeliveryFor; }
        }

        /// <summary>
        /// Address where to send this message.
        /// </summary>
        public string Destination
        {
            get { return destination; }
        }
    }
}