namespace NServiceBus
{
    using System;

    class RootContext : BehaviorContext
    {
        public RootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache) : base(null)
        {
            Set(messageOperations);
            Set(builder);
            Set(pipelineCache);
        }
    }
}