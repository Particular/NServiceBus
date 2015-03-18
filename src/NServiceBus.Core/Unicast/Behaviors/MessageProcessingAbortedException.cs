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
        /// 
        /// </summary>
        public MessageProcessingAbortedException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MessageProcessingAbortedException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}