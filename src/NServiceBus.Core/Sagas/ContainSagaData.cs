namespace NServiceBus
{
    using System;

    /// <summary>
    /// Base class to make defining saga data easier.
    /// </summary>
    public abstract class ContainSagaData : IContainSagaData
    {
        /// <summary>
        /// The saga id.
        /// </summary>
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The address io the endpoint that started the saga.
        /// </summary>
        public virtual string Originator { get; set; }

        /// <summary>
        /// The id of the message that started the saga.
        /// </summary>
        public virtual string OriginalMessageId { get; set; }
    }
}