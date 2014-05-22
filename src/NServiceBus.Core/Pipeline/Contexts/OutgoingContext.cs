namespace NServiceBus.Pipeline.Contexts
{
    using Unicast;
    using Unicast.Messages;

    public class OutgoingContext : BehaviorContext
    {
        public OutgoingContext(BehaviorContext parentContext, DeliveryOptions deliveryOptions, LogicalMessage message)
            : base(parentContext)
        {
            Set(deliveryOptions);
            Set(OutgoingLogicalMessageKey, message);
        }

        public DeliveryOptions DeliveryOptions
        {
            get { return Get<DeliveryOptions>(); }
        }

        public LogicalMessage OutgoingLogicalMessage
        {
            get { return Get<LogicalMessage>(OutgoingLogicalMessageKey); }
        }

        public TransportMessage IncomingMessage
        {
            get
            {
                TransportMessage message;

                //todo: I think we should move to strongly typed parent contexts so the below should be
                // parentContext.IncomingMessage or similar
                parentContext.TryGet(IncomingContext.IncomingPhysicalMessageKey, out message);

                return message;
            }
        }

        public TransportMessage OutgoingMessage
        {
            get { return Get<TransportMessage>(); }
        }

        const string OutgoingLogicalMessageKey = "NServiceBus.OutgoingLogicalMessage";
    }
}