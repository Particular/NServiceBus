#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        _ = context.Extensions.TryGet<LogicalMessage>(out var incomingLogicalMessage);
        _ = context.Extensions.TryGet<IncomingMessage>(out var incomingPhysicalMessage);

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
            UpdateMessage(context, mutatorContext);
        }

        await next(context).ConfigureAwait(false);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Path without compiler-known type can only be visited if MutateOutgoingMessageContext.OutgoingMessage setter is used.")]
#pragma warning disable PS0015 // Multiple cancellable contexts are fine here
    static void UpdateMessage(IOutgoingLogicalMessageContext context, MutateOutgoingMessageContext mutatorContext)
#pragma warning restore PS0015
    {
        if (mutatorContext.ReplacementMessageType != null)
        {
            context.UpdateMessage(mutatorContext.OutgoingMessage, mutatorContext.ReplacementMessageType);
        }
        else
        {
            // Requires code path to use MutateOutgoingMessageContext.OutgoingMessage which is marked as RequiresUnreferencedCode
            context.UpdateMessage(mutatorContext.OutgoingMessage);
        }
    }

    volatile bool hasOutgoingMessageMutators = true;
}