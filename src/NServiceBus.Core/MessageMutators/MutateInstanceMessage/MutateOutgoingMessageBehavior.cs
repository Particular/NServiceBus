#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0025:Dictionary keys should implement GetHashCode", Justification = "Mutators are registered based on reference equality")]
class MutateOutgoingMessageBehavior(HashSet<IMutateOutgoingMessages> mutators) : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
{
    public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        => hasOutgoingMessageMutators ? InvokeOutgoingMessageMutators(context, next) : next(context);

    async Task InvokeOutgoingMessageMutators(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
    {
        _ = context.Extensions.TryGet(out LogicalMessage? incomingLogicalMessage);
        _ = context.Extensions.TryGet(out IncomingMessage? incomingPhysicalMessage);

        var mutatorContext = new MutateOutgoingMessageContext(
            context.Message.Instance,
            context.Headers,
            incomingLogicalMessage?.Instance,
            incomingPhysicalMessage?.Headers,
            context.CancellationToken);

        var hasMutators = false;

        foreach (var mutator in context.Builder.GetServices<IMutateOutgoingMessages>())
        {
            hasMutators = true;

            await mutator.MutateOutgoing(mutatorContext)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }

        foreach (var mutator in mutators)
        {
            hasMutators = true;

            await mutator.MutateOutgoing(mutatorContext)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }

        hasOutgoingMessageMutators = hasMutators;

        if (mutatorContext.MessageInstanceChanged)
        {
            context.UpdateMessage(mutatorContext.OutgoingMessage);
        }

        await next(context).ConfigureAwait(false);
    }

    volatile bool hasOutgoingMessageMutators = true;
}