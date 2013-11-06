namespace NServiceBus.Pipeline
{
    using ObjectBuilder;

    internal class PhysicalMessageContext : BehaviorContext
    {
        public PhysicalMessageContext(IBuilder builder, BehaviorContext parentContext, TransportMessage transportMessage)
            : base(builder,parentContext)
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