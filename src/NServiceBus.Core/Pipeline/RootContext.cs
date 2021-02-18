namespace NServiceBus
{
    using System;
    using System.Threading;

    class RootContext : BehaviorContext
    {
        public RootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache, CancellationToken cancellationToken)
            : base(null, cancellationToken)
        {
            Set(messageOperations);
            Set(builder);
            Set(pipelineCache);
        }
    }
}