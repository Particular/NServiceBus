namespace NServiceBus.Pipeline
{
    using Behaviors;
    using ObjectBuilder;

    internal class LogicalMessageContext : BehaviorContext
    {
        public LogicalMessageContext(IBuilder builder,BehaviorContext parentContext, LogicalMessage message)
            : base(builder, parentContext)
        {
            Set(message);
        }

        public LogicalMessage LogicalMessage
        {
            get { return Get<LogicalMessage>(); }
        }
    }
}