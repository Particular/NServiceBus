namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Unicast;
    using Unicast.Messages;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SendLogicalMessagesContext : BehaviorContext
    {
        public SendLogicalMessagesContext(BehaviorContext parentContext, SendOptions sendOptions, IEnumerable<LogicalMessage> messages)
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

                parentContext.TryGet(ReceivePhysicalMessageContext.IncomingPhysicalMessageKey, out message);

                return message;
            }
        }
    }
}