namespace NServiceBus.Pipeline
{
    internal class PhysicalMessageContext : BehaviorContext
    {
        public PhysicalMessageContext(PipelineFactory pipelineFactory,TransportMessage transportMessage):base(pipelineFactory)
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