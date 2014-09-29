namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception used to transport exceptions encountered in message handlers.
    /// </summary>
    [Serializable]
    [ObsoleteEx(
        Message = "If multiple pipeline exceptions occur they will now be wrapped in an AggregateException.",
        TreatAsErrorFromVersion = "4.7",
        RemoveInVersion = "5.0")]
    public class TransportMessageHandlingFailedException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="originalException">The exception that got thrown from the message handler.</param>
        public TransportMessageHandlingFailedException(Exception originalException)
            : base("An exception was thrown by the message handler.", originalException)
        {
        }

        protected TransportMessageHandlingFailedException(SerializationInfo info, StreamingContext context)
        {
            
        }

    }
}