namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// This exception will bypass any retries and will cause the message to be moved to the error queue immediately.
    /// </summary>
    [Serializable]
    public class UnrecoverableException : Exception
    {
        /// <summary>
        /// Initializes an UnrecoverableException, causing the message to be moved to the error queue immediately.
        /// </summary>
        public UnrecoverableException()
        {

        }

        /// <summary>
        /// Initializes an UnrecoverableException, causing the message to be moved to the error queue immediately.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public UnrecoverableException(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes an UnrecoverableException, causing the message to be moved to the error queue immediately.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public UnrecoverableException(string message, Exception innerException) : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes an UnrecoverableException, causing the message to be moved to the error queue immediately.
        /// </summary>
        protected UnrecoverableException(SerializationInfo info, StreamingContext context)
        {

        }
    }
}