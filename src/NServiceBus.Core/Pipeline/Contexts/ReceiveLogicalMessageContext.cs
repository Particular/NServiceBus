namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.ComponentModel;
    using Unicast.Messages;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ReceiveLogicalMessageContext : BehaviorContext
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