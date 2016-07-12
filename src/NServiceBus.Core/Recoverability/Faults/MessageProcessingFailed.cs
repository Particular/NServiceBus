namespace NServiceBus
{
    using Transport;

    abstract class MessageProcessingFailed
    {
        public IncomingMessage Message { get; }
        public ExceptionInfo ExceptionInfo { get; }

        protected MessageProcessingFailed(IncomingMessage message, ExceptionInfo exceptionInfo)
        {
            Message = message;
            ExceptionInfo = exceptionInfo;
        }
    }
}