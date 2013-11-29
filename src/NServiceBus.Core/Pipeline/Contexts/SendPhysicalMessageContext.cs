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
    public class SendPhysicalMessageContext : BehaviorContext
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