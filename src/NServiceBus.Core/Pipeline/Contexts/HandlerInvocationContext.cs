namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.ComponentModel;
    using Unicast.Behaviors;
    using Unicast.Messages;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
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