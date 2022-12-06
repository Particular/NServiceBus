namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;

    //TODO rename to PipelineRootContext?
    class RootContext : BehaviorContext
    {
        public RootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache, CancellationToken cancellationToken, ContextBag parentContext = null)
            : base(parentContext, cancellationToken)
        {
            Set(messageOperations);
            Set(builder);
            Set(pipelineCache);
        }
    }
}