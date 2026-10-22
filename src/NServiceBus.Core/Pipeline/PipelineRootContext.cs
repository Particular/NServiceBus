#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using Extensibility;
using Transport;

class PipelineRootContext : BehaviorContext
{
    public PipelineRootContext(IServiceProvider builder, MessageOperations messageOperations, IPipelineCache pipelineCache, CancellationToken cancellationToken, ContextBag? parentContext = null)
        : base(parentContext, cancellationToken)
    {
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

    internal void DeferHeaderReturn(HeaderPool pool, Dictionary<string, string> headers) => pool.TryDefer(headers, ref deferredHeaders);

    internal void ReturnDeferredHeaders(HeaderPool pool) => pool.ReturnDeferred(ref deferredHeaders);

    Dictionary<string, string>? deferredHeaders;
}