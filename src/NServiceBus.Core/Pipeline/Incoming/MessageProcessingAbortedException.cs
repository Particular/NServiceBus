namespace NServiceBus.Transports
{
    using System;
    using System.Runtime.Serialization;
    using JetBrains.Annotations;

    /// <summary>
    /// When thrown during pipeline invocation indicates that <see cref="IPushMessages"/> should return message to the input queue causing any recive transactions to rollback.
    /// </summary>
    [Serializable]
    public class MessageProcessingAbortedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        public MessageProcessingAbortedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        protected MessageProcessingAbortedException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}