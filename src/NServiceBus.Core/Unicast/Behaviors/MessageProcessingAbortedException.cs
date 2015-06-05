namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Informs that message processing has been aborted.
    /// </summary>
    [Serializable]
    public class MessageProcessingAbortedException : Exception
    {
        /// <summary>
        /// Initializes a new insatnce of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        public MessageProcessingAbortedException()
        {
        }

        /// <summary>
        /// Initializes a new insatnce of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        protected MessageProcessingAbortedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}