namespace NServiceBus.Pipeline.Contexts
{
    class ReceivePhysicalMessageContext : BehaviorContext
    {
        public ReceivePhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
        }

        public static string IncomingPhysicalMessageKey
        {
            get { return "NServiceBus.IncomingPhysicalMessage"; }
        }
    }
}