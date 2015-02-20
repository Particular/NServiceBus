namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// Wraps the <see cref="Exception"/> that occurs when the contents of a <see cref="TransportMessage"/> is deserialized to a list of <see cref="LogicalMessage"/>s.
    /// </summary>
    [Serializable]
    public class MessageDeserializationException : SerializationException
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public MessageDeserializationException(string message):base(message)

        {
            
        }
        /// <summary>
        /// Initializes a new instance of <see cref="MessageDeserializationException"/>.
        /// </summary>
        /// <param name="innerException"> The exception that is the cause of the current exception.</param>
        /// <param name="transportMessageId">The id of the <see cref="TransportMessage"/> that failed to deserialize.</param>
        public MessageDeserializationException(string  transportMessageId, Exception innerException)
            : base("An error occurred while attempting to extract logical messages from transport message " + transportMessageId, innerException)
        {
            
        }

        /// <summary>
        /// <see cref="SerializationException(SerializationInfo, StreamingContext)"/>
        /// </summary>
        protected MessageDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}