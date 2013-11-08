namespace NServiceBus.Pipeline
{
    using System.Collections.Generic;
    using Behaviors;
    using Unicast;

    internal class SendLogicalMessagesContext : BehaviorContext
    {
        public SendLogicalMessagesContext(BehaviorContext parentContext, SendOptions sendOptions,IEnumerable<LogicalMessage> messages)
            : base(parentContext)
        {
            Set(sendOptions);
            Set(messages);
        }

        public SendOptions SendOptions
        {
            get { return Get<SendOptions>(); }
        }

        public IEnumerable<LogicalMessage> LogicalMessages
        {
            get { return Get<IEnumerable<LogicalMessage>>(); }
        }
    }
}