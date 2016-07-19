namespace NServiceBus
{
    using System;
    using Transport;

    class MessageFaulted : MessageProcessingFailed
    {
        public string ErrorQueue { get; }

        public MessageFaulted(IncomingMessage message, Exception exception, string errorQueue) : base(message, exception)
        {
            ErrorQueue = errorQueue;
        }
    }
}