namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for options to deliver messages.
    /// </summary>
    public abstract class DeliveryOptions
    {
        /// <summary>
        /// Creates an instance of <see cref="DeliveryOptions"/>.
        /// </summary>
        protected DeliveryOptions()
        {
            EnforceMessagingBestPractices = true;
            EnlistInReceiveTransaction = true;
            Headers = new Dictionary<string, string>();
        }

        /// <summary>
        /// If set messaging best practices will be enforces (on by default)
        /// </summary>
        public bool EnforceMessagingBestPractices { get; set; }

        /// <summary>
        /// Tells the transport to enlist the outgoing operation in the current receive transaction if possible.
        /// This is enabled by default
        /// </summary>
        public bool EnlistInReceiveTransaction { get; set; }
        
        /// <summary>
        /// The reply address to use for outgoing messages
        /// </summary>
        public string ReplyToAddress { get; set; }


        /// <summary>
        /// The TTBR to use for this message
        /// </summary>
        public TimeSpan? TimeToBeReceived { get; set; }


        /// <summary>
        /// Controls if the transport should be requested to handle the message in a way that it survives restarts
        /// </summary>
        public bool? NonDurable { get; set; }

        /// <summary>
        /// The headers for the message
        /// </summary>
        public Dictionary<string,string> Headers { get; set; }
    }
}