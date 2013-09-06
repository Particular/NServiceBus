namespace NServiceBus.GatewayPersister.NHibernate.Config
{
    using System;

    /// <summary>
    /// The Gateway message
    /// </summary>
    public class GatewayMessage
    {
        /// <summary>
        /// Id of this message.
        /// </summary>
        public virtual string Id { get; set; }

        /// <summary>
        /// Store the headers to preserve them across timeouts.
        /// </summary>
        public virtual string Headers { get; set; }

        /// <summary>
        /// The time at which the message was received.
        /// </summary>
        public virtual DateTime TimeReceived { get; set; }

        /// <summary>
        /// The original message.
        /// </summary>
        public virtual byte[] OriginalMessage { get; set; }

        /// <summary>
        /// Acknowledgment that the message was successfully received.
        /// </summary>
        public virtual bool Acknowledged { get; set; }
    }
}