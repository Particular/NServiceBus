namespace NServiceBus.Pipeline
{
    using Behaviors;

    internal class LogicalMessageContext : BehaviorContext
    {
        public LogicalMessageContext(PipelineFactory pipelineFactory,BehaviorContext parentContext, LogicalMessage message)
            : base(pipelineFactory,parentContext)
        {
            Set(message);
        }

        public LogicalMessage LogicalMessage
        {
            get { return Get<LogicalMessage>(); }
        }
    }
}