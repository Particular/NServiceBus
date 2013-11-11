namespace NServiceBus.Pipeline.Contexts
{
    using Unicast.Messages;

    class LogicalMessageContext : BehaviorContext
    {
        public LogicalMessageContext(BehaviorContext parentContext, LogicalMessage message)
            : base(parentContext)
        {
            Set(message);
        }

        public LogicalMessage LogicalMessage
        {
            get { return Get<LogicalMessage>(); }
        }
    }
}