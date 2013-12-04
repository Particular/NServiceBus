namespace NServiceBus.Pipeline.Contexts
{
    using Unicast;
    using Unicast.Messages;

    class SendLogicalMessageContext : BehaviorContext
    {
        public SendLogicalMessageContext(BehaviorContext parentContext, SendOptions sendOptions, LogicalMessage message)
            : base(parentContext)
        {
            Set(sendOptions);
            Set(message);
        }

        public SendOptions SendOptions
        {
            get { return Get<SendOptions>(); }
        }

        public LogicalMessage MessageToSend
        {
            get
            {
                return Get<LogicalMessage>();
            }
        }

        public TransportMessage IncomingMessage
        {
            get
            {
                TransportMessage message;

                //todo: I think we should move to strongly typed parent contexts so the below should be
                // parentContext.IncomingMessage or similar
                parentContext.TryGet(ReceivePhysicalMessageContext.IncomingPhysicalMessageKey, out message);

                return message;
            }
        }
    }
}