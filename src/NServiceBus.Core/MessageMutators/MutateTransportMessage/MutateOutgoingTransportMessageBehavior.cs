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
class MutateOutgoingTransportMessageBehavior(HashSet<IMutateOutgoingTransportMessages> mutators)
    : IBehavior<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>
{
    public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
        => hasOutgoingTransportMessageMutators ? InvokeOutgoingTransportMessageMutators(context, next) : next(context);

    async Task InvokeOutgoingTransportMessageMutators(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
    {
        var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();

        _ = context.Extensions.TryGet(out LogicalMessage? incomingLogicalMessage);
        _ = context.Extensions.TryGet(out IncomingMessage? incomingPhysicalMessage);

        var mutatorContext = new MutateOutgoingTransportMessageContext(
            context.Body,
            outgoingMessage.Instance,
            context.Headers,
            incomingLogicalMessage?.Instance,
            incomingPhysicalMessage?.Headers,
            context.CancellationToken);

        var hasMutators = false;

        foreach (var mutator in context.Builder.GetServices<IMutateOutgoingTransportMessages>())
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

        hasOutgoingTransportMessageMutators = hasMutators;

        if (mutatorContext.MessageBodyChanged)
        {
            context.UpdateMessage(mutatorContext.OutgoingBody);
        }

        await next(context).ConfigureAwait(false);
    }

    volatile bool hasOutgoingTransportMessageMutators = true;
}