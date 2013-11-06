namespace NServiceBus.Pipeline
{
    internal class PhysicalMessageContext : BehaviorContext
    {
        public PhysicalMessageContext(PipelineFactory pipelineFactory, BehaviorContext parentContext, TransportMessage transportMessage)
            : base(pipelineFactory,parentContext)
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