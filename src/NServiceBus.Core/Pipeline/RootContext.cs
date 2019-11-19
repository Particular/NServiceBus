namespace NServiceBus
{
    using ObjectBuilder;

    class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder, MessageOperations messageOperations, IPipelineCache pipelineCache) : base(null)
        {
            Set(messageOperations);
            Set(builder);
            Set(pipelineCache);
        }
    }
}