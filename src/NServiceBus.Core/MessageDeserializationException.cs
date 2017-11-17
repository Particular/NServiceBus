namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using Pipeline;
    using Transport;

    /// <summary>
    /// Wraps the <see cref="Exception" /> that occurs when the contents of an <see cref="IncomingMessage" /> is deserialized
    /// to a list of <see cref="LogicalMessage" />s.
    /// </summary>
    [Serializable]
    public class MessageDeserializationException : SerializationException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MessageDeserializationException" />.
        /// </summary>
        public MessageDeserializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessageDeserializationException" />.
        /// </summary>
        /// <param name="innerException"> The exception that is the cause of the current exception.</param>
        /// <param name="messageId">The id of the <see cref="IncomingMessage" /> that failed to deserialize.</param>
        public MessageDeserializationException(string messageId, Exception innerException)
            : base("An error occurred while attempting to extract logical messages from incoming physical message " + messageId, innerException)
        {
        }

        /// <summary>
        /// <see cref="SerializationException(SerializationInfo, StreamingContext)" />.
        /// </summary>
        #pragma warning disable PC001
        protected MessageDeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        #pragma warning restore PC001
    }
}