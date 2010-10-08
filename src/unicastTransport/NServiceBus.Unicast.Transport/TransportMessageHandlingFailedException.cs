using System;

namespace NServiceBus.Unicast.Transport
{
    /// <summary>
    /// Exception used to transport exceptions encountered in messagehandlers
    /// </summary>
    public class TransportMessageHandlingFailedException : Exception
    {
        /// <summary>
        /// The exception that got thrown from the messagehandler
        /// </summary>
        public Exception OriginalException { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="originalException"></param>
        public TransportMessageHandlingFailedException(Exception originalException)
        {
            OriginalException = originalException;
        }
    }
}