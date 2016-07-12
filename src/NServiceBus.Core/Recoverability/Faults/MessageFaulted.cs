namespace NServiceBus
{
    using System;
    using Transport;

    class MessageFaulted : MessageProcessingFailed
    {
        public MessageFaulted(IncomingMessage message, Exception exception) : base(message, exception)
        {
        }
    }
}