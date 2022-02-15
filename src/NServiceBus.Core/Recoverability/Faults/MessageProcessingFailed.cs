namespace NServiceBus
{
    using System;
    using Transport;

    abstract class MessageProcessingFailed
    {
        public IncomingMessage Message { get; }
        public Exception Exception { get; }

        protected MessageProcessingFailed(IncomingMessage failedMessage, Exception exception)
        {
            Message = failedMessage;
            Exception = exception;
        }
    }
}