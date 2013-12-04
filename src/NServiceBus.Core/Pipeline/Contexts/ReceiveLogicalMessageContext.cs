namespace NServiceBus.Pipeline.Contexts
{
    using Unicast.Messages;

    class ReceiveLogicalMessageContext : BehaviorContext
    {
        public ReceiveLogicalMessageContext(BehaviorContext parentContext, LogicalMessage message)
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