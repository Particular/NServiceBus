namespace NServiceBus
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Base class to make defining saga data easier.
    /// </summary>
    public abstract class ContainSagaData : IContainSagaData
    {
        /// <summary>
        /// The saga id.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Guid Id { get; set; }

        /// <summary>
        /// The address io the endpoint that started the saga.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string Originator { get; set; }

        /// <summary>
        /// The id of the message that started the saga.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string OriginalMessageId { get; set; }
    }
}