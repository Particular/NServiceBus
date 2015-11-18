namespace NServiceBus.Testing.Fakes
{
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    public class TestableTransportReceiveContext : TransportReceiveContext
    {
        public TestableTransportReceiveContext(IncomingMessage receivedMessage, PipelineInfo pipelineInfo, BehaviorContext parentContext) 
            : base(receivedMessage, pipelineInfo, parentContext)
        {
            // no overrides
        }
    }
}