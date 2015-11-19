namespace NServiceBus.Testing.Fakes
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    public class TestableTransportReceiveContext : TransportReceiveContext
    {
        public ContextBag Extensions { get; }
        public IBuilder Builder { get; }
        public IncomingMessage Message { get; }
        public PipelineInfo PipelineInfo { get; }
    }
}