namespace NServiceBus
{
    using System;
    using Transport;

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