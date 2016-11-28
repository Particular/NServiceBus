namespace NServiceBus
{
    using System;
    using Transport;

    abstract class MessageProcessingFailed
    {
        public IncomingMessage Message { get; }
        public Exception Exception { get; }
        public ErrorContext ErrorContext { get; }

        protected MessageProcessingFailed(ErrorContext errorContext)
        {
            Message = errorContext.Message;
            Exception = errorContext.Exception;
            ErrorContext = errorContext;
        }
    }
}