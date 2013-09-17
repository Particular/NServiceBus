namespace NServiceBus.Deduplication.NHibernate.Config
{
    using System;

    /// <summary>
    /// The Gateway message
    /// </summary>
    public class DeduplicationMessage
    {
        /// <summary>
        /// Id of this message.
        /// </summary>
        public virtual string Id { get; set; }

        /// <summary>
        /// The time at which the message was received.
        /// </summary>
        public virtual DateTime TimeReceived { get; set; }
    }
}