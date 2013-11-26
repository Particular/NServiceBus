namespace NServiceBus.Pipeline.Contexts
{
    class ReceivePhysicalMessageContext : BehaviorContext
    {
        public ReceivePhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage, bool messageHandlingDisabled)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, transportMessage);
            Set("MessageHandlingDisabled", messageHandlingDisabled);
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(IncomingPhysicalMessageKey); }
        }

        public bool MessageHandlingDisabled
        {
            get { return Get<bool>("MessageHandlingDisabled"); }
        }

        public static string IncomingPhysicalMessageKey
        {
            get { return "NServiceBus.IncomingPhysicalMessage"; }
        }
    }
}