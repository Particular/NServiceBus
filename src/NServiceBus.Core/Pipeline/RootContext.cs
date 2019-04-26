namespace NServiceBus
{
    using MessageInterfaces;
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder, IPipelineCache pipelineCache, IEventAggregator eventAggregator, IMessageMapper messageMapper) : base(null)
        {
            Set(builder);
            Set(pipelineCache);
            Set(eventAggregator);
            Set(messageMapper);
        }
    }
}