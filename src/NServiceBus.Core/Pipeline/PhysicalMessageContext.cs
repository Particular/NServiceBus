namespace NServiceBus.Pipeline
{
    internal class PhysicalMessageContext : BehaviorContext
    {
        public PhysicalMessageContext(BehaviorContext parentContext, TransportMessage transportMessage)
            : base(parentContext)
        {
            handleCurrentMessageLaterWasCalled = false;

            Set(transportMessage);
        }

        public TransportMessage PhysicalMessage
        {
            get { return Get<TransportMessage>(); }
        }
    }
}