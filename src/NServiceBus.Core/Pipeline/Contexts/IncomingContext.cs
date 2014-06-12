namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast.Behaviors;
    using Unicast.Messages;

    public class IncomingContext : BehaviorContext
    {
        public IncomingContext(BehaviorContext parentContext, TransportMessage transportMessage)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);

            LogicalMessages = new List<LogicalMessage>();
        }

        public bool HandlerInvocationAborted { get; private set; }

        public void DoNotInvokeAnyMoreHandlers()
        {
            HandlerInvocationAborted = true;
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
        }
        
        public List<LogicalMessage> LogicalMessages
        {
            get { return Get<List<LogicalMessage>>(); }
            set { Set(value); }
        }

        public LogicalMessage IncomingLogicalMessage
        {
            get { return Get<LogicalMessage>(IncomingLogicalMessageKey); }
            set { Set(IncomingLogicalMessageKey, value); }
        }

        public MessageHandler MessageHandler
        {
            get { return Get<MessageHandler>(); }
            set { Set(value); }
        }

        public const string IncomingPhysicalMessageKey = "NServiceBus.IncomingPhysicalMessage";
        const string IncomingLogicalMessageKey = "NServiceBus.IncomingLogicalMessageKey";
    }
}