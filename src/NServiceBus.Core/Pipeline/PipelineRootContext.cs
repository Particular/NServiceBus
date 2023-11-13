namespace NServiceBus;

using System;
using System.Threading;
using Extensibility;

class PipelineRootContext : BehaviorContext
{
    public PipelineRootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache, CancellationToken cancellationToken, ContextBag parentContext = null)
        : base(parentContext, cancellationToken)
    {
        Set(messageOperations);
        Set(builder);
        Set(pipelineCache);
    }
}