namespace NServiceBus
{
    using ObjectBuilder;

    /// <summary>
    /// The root context.
    /// </summary>
    class RootContext : BehaviorContext
    {
        public RootContext(IChildBuilder builder, IPipelineCache pipelineCache) : base(null)
        {
            Set(builder);
            Set(pipelineCache);
        }
    }
}