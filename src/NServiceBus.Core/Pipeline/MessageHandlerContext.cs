namespace NServiceBus.Pipeline
{
    using Behaviors;

    internal class MessageHandlerContext : BehaviorContext
    {
        public MessageHandlerContext(PipelineFactory pipelineFactory, BehaviorContext parentContext, MessageHandler messageHandler)
            : base(pipelineFactory,parentContext)
        {
            Set(messageHandler);
        }

        public MessageHandler MessageHandler
        {
            get { return Get<MessageHandler>(); }
        }

        public LogicalMessage LogicalMessage
        {
            get { return Get<LogicalMessage>(); }
        }
    }
}