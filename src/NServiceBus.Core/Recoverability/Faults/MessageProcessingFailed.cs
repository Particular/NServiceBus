namespace NServiceBus
{
    using System;
    using Transports;

    abstract class MessageProcessingFailed
    {
        public IncomingMessage Message { get; }
        public Exception Exception { get; }

        protected MessageProcessingFailed(IncomingMessage message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}