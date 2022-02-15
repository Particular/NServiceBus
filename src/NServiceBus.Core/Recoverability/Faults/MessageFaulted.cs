namespace NServiceBus
{
    using System;
    using Transport;

    class MessageFaulted : MessageProcessingFailed
    {
        public string ErrorQueue { get; }

        public MessageFaulted(string errorQueue, IncomingMessage failedMessage, Exception exception) : base(failedMessage, exception)
        {
            ErrorQueue = errorQueue;
        }
    }
}