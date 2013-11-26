namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Messages;

    class SendLogicalMessagesContext : BehaviorContext
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

        public TransportMessage IncomingMessage
        {
            get
            {
                TransportMessage message;

                parentContext.TryGet(IncomingPhysicalMessageContext.IncomingPhysicalMessageKey, out message);

                return message;
            }
        }
    }
}