namespace NServiceBus.Pipeline.Contexts
{
    class IncomingPhysicalMessageContext : BehaviorContext
    {
        public IncomingPhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage)
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