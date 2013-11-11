namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Messages;

    internal class SendPhysicalMessageContext : BehaviorContext
    {
        public SendPhysicalMessageContext(BehaviorContext parentContext, SendOptions sendOptions, TransportMessage message)
            : base(parentContext)
        {
            Set(sendOptions);
            Set(message);
        }

        public SendOptions SendOptions
        {
            get { return Get<SendOptions>(); }
        }

        public TransportMessage MessageToSend
        {
            get
            {
                return Get<TransportMessage>();
            }
        }

        public IEnumerable<LogicalMessage> LogicalMessages
        {
            get
            {
                IEnumerable<LogicalMessage> result;

                if (TryGet(out result))
                {
                    return result;
                }

                return new List<LogicalMessage>();
            }
        }
    }
}