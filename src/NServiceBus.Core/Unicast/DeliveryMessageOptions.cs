namespace NServiceBus.Unicast
{
    using System;

    /// <summary>
    /// Options to deliver messages.
    /// </summary>
    public class DeliveryMessageOptions
    {
        /// <summary>
        /// Creates an instance of <see cref="DeliveryMessageOptions"/>.
        /// </summary>
        public DeliveryMessageOptions()
        {
            EnlistInReceiveTransaction = true;
        }

    
        /// <summary>
        /// Tells the transport to enlist the outgoing operation in the current receive transaction if possible.
        /// This is enabled by default
        /// </summary>
        public bool EnlistInReceiveTransaction { get; set; }

        /// <summary>
        /// The TTBR to use for this message
        /// </summary>
        public TimeSpan? TimeToBeReceived { get; set; }

        /// <summary>
        /// Controls if the transport should be requested to handle the message in a way that it survives restarts
        /// </summary>
        public bool? NonDurable { get; set; }
    }
}