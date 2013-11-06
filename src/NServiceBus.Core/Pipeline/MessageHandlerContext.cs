namespace NServiceBus.Pipeline
{
    using Behaviors;

    internal class MessageHandlerContext : BehaviorContext
    {
        public MessageHandlerContext(BehaviorContext parentContext, MessageHandler messageHandler)
            : base(parentContext)
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