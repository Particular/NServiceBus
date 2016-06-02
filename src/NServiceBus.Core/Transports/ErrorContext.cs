namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Extensibility;

    /// <summary>
    ///
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        ///
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 
        /// </summary>
        public int NumberOfProcessingAttempts { get; }

        /// <summary>
        /// The ID of the message that failed processing.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public Stream BodyStream { get; } 

        /// <summary>
        /// 
        /// </summary>
        public ContextBag Context { get;  }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="numberOfProcessingAttempts"></param>
        /// <param name="messageId"></param>
        /// <param name="headers"></param>
        /// <param name="bodyStream"></param>
        /// <param name="context"></param>
        public ErrorContext(Exception exception, int numberOfProcessingAttempts, string messageId, Dictionary<string, string> headers, Stream bodyStream, ContextBag context)
        {
            Exception = exception;
            NumberOfProcessingAttempts = numberOfProcessingAttempts;
            MessageId = messageId;
            Headers = headers;
            BodyStream = bodyStream;
            Context = context;
        }
    }
}