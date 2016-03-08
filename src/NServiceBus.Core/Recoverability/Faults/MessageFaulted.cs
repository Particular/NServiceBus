namespace NServiceBus
{
    using System;
    using Transports;

    class MessageFaulted : MessageProcessingFailed
    {
        public MessageFaulted(IncomingMessage message, Exception exception) : base(message, exception)
        {
        }
    }
}