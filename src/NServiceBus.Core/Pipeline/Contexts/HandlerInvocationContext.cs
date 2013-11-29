namespace NServiceBus.Pipeline.Contexts
{
    using System.ComponentModel;
    using Unicast.Behaviors;
    using Unicast.Messages;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class HandlerInvocationContext : BehaviorContext
    {
        public HandlerInvocationContext(BehaviorContext parentContext, MessageHandler messageHandler)
            : base(parentContext)
        {
            Set(messageHandler);
        }

        public MessageHandler MessageHandler
        {
            get { return Get<MessageHandler>(); }
        }

        public LogicalMessage LogicalMessage
        {
            get { return Get<LogicalMessage>(); }
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(ReceivePhysicalMessageContext.IncomingPhysicalMessageKey); }
        }
    }
}