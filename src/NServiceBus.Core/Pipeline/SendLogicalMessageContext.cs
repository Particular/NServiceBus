namespace NServiceBus.Pipeline
{
    using Behaviors;
    using Unicast;

    internal class SendLogicalMessageContext : BehaviorContext
    {
        public SendLogicalMessageContext(BehaviorContext parentContext, SendOptions sendOptions, LogicalMessage message)
            : base(parentContext)
        {
            Set(sendOptions);
            Set(message);
        }

        public SendOptions SendOptions
        {
            get { return Get<SendOptions>(); }
        }

        public LogicalMessage MessageToSend
        {
            get
            {
                return Get<LogicalMessage>();
            }
        }
    }
}