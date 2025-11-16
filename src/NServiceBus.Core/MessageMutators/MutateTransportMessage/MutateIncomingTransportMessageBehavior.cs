#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0025:Dictionary keys should implement GetHashCode", Justification = "Mutators are registered based on reference equality")]
class MutateIncomingTransportMessageBehavior(HashSet<IMutateIncomingTransportMessages> mutators)
    : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next) => hasIncomingTransportMessageMutators ? InvokeIncomingTransportMessagesMutators(context, next) : next(context);

    async Task InvokeIncomingTransportMessagesMutators(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        var mutatorsRegisteredInDI = context.Builder.GetServices<IMutateIncomingTransportMessages>();
        var transportMessage = context.Message;
        var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body, transportMessage.Headers, context.CancellationToken);

        var hasMutators = false;

        foreach (var mutator in mutatorsRegisteredInDI)
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

        hasIncomingTransportMessageMutators = hasMutators;

        if (mutatorContext.MessageBodyChanged)
        {
            context.UpdateMessage(mutatorContext.Body);
        }

        await next(context).ConfigureAwait(false);
    }

    volatile bool hasIncomingTransportMessageMutators = true;
}