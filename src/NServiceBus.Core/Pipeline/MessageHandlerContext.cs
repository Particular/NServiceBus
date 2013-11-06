namespace NServiceBus.Pipeline
{
    using Behaviors;
    using ObjectBuilder;

    internal class MessageHandlerContext : BehaviorContext
    {
        public MessageHandlerContext(IBuilder builder, BehaviorContext parentContext, MessageHandler messageHandler)
            : base(builder, parentContext)
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