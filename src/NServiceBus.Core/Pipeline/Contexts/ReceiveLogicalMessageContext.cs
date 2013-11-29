namespace NServiceBus.Pipeline.Contexts
{
    using System.ComponentModel;
    using Unicast.Messages;


    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
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