#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0025:Dictionary keys should implement GetHashCode", Justification = "Mutators are registered based on reference equality")]
class MutateIncomingMessageBehavior(HashSet<IMutateIncomingMessages> mutators)
    : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
{
    public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next) => hasIncomingMessageMutators ? InvokeIncomingMessageMutators(context, next) : next(context);

    async Task InvokeIncomingMessageMutators(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
    {
        var logicalMessage = context.Message;
        var current = logicalMessage.Instance;

        var mutatorContext = new MutateIncomingMessageContext(current, context.Headers, context.CancellationToken);

        var hasMutators = false;

        foreach (var mutator in context.Builder.GetServices<IMutateIncomingMessages>())
        {
            hasMutators = true;

            await mutator.MutateIncoming(mutatorContext)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }

        foreach (var mutator in mutators)
        {
            hasMutators = true;

            await mutator.MutateIncoming(mutatorContext)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }

        hasIncomingMessageMutators = hasMutators;

        if (mutatorContext.MessageInstanceChanged)
        {
            UpdateMessageInstance(context, mutatorContext);
        }

        await next(context).ConfigureAwait(false);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026",
        Justification = "Path without compiler-known type can only be visited if MutateIncomingMessageContext.Message setter is used.")]
#pragma warning disable PS0015 // Multiple cancellable contexts are fine here
    static void UpdateMessageInstance(IIncomingLogicalMessageContext context, MutateIncomingMessageContext mutatorContext)
#pragma warning restore PS0015
    {
        if (mutatorContext.ReplacementMessageType != null)
        {
            context.UpdateMessageInstance(mutatorContext.Message, mutatorContext.ReplacementMessageType);
        }
        else
        {
            // Requires code path to use MutateIncomingMessageContext.Message which is marked as RequiresUnreferencedCode
            context.UpdateMessageInstance(mutatorContext.Message);
        }
    }

    volatile bool hasIncomingMessageMutators = true;
}