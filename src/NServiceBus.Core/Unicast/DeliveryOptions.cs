namespace NServiceBus.Unicast
{
    public abstract class DeliveryOptions
    {
        protected DeliveryOptions()
        {
            EnforceMessagingBestPractices = true;
            EnlistInReceiveTransaction = true;
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
        public Address ReplyToAddress { get; set; }
    }
}