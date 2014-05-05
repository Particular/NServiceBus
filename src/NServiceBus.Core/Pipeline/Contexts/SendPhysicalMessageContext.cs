namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.ComponentModel;
    using Unicast;
    using Unicast.Messages;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
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

        public LogicalMessage LogicalMessage
        {
            get
            {
                LogicalMessage result;

                if (TryGet(out result))
                {
                    return result;
                }

                return null;
            }
        }
    }
}