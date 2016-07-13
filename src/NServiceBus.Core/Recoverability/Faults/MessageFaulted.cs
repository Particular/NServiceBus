namespace NServiceBus
{
    using Transport;

    class MessageFaulted : MessageProcessingFailed
    {
        public MessageFaulted(IncomingMessage message, ExceptionInfo exceptionInfo) : base(message, exceptionInfo)
        {
        }
    }
}