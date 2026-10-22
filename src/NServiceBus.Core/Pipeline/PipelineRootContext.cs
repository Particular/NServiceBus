#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using Extensibility;
using Transport;

class PipelineRootContext : BehaviorContext
{
    public PipelineRootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache, HeaderPool headerPool, CancellationToken cancellationToken, ContextBag? parentContext = null)
        : base(parentContext, cancellationToken)
    {
        this.headerPool = headerPool;
        Set(messageOperations);
        Set(builder);
        Set(pipelineCache);
    }

    internal static PipelineRootContext? FindOwner(ContextBag context)
    {
        for (var bag = context; bag is not null; bag = bag.parentBag)
        {
            if (bag is PipelineRootContext owner)
            {
                return owner;
            }
        }

        return null;
    }

    internal void DeferHeaderReturn(Dictionary<string, string> headers) => headerPool.TryDefer(headers, ref deferredHeaders);

    internal void ReturnDeferredHeaders() => headerPool.ReturnDeferred(ref deferredHeaders);

    internal HeaderReturnScope TrackHeaderReturns() => new(this);

    readonly HeaderPool headerPool;
    Dictionary<string, string>? deferredHeaders;

    // Marks where the invocation that owns the deferred headers ends; disposing returns them to the pool.
    internal readonly struct HeaderReturnScope(PipelineRootContext root) : IDisposable
    {
        public void Dispose() => root.ReturnDeferredHeaders();
    }
}