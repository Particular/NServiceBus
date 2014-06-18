namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception used to transport exceptions encountered in message handlers.
    /// </summary>
    [Serializable]
    public class TransportMessageHandlingFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransportMessageHandlingFailedException"/> class.
        /// </summary>
        /// <param name="originalException">The exception that got thrown from the message handler.</param>
        public TransportMessageHandlingFailedException(Exception originalException) : base("An exception was thrown by the message handler.", originalException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransportMessageHandlingFailedException"/> class with serialized data.
        /// </summary>
        protected TransportMessageHandlingFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}