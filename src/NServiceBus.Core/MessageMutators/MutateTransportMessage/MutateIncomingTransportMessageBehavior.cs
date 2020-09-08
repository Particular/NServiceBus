﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MessageMutator;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;

    class MutateIncomingTransportMessageBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public MutateIncomingTransportMessageBehavior(HashSet<IMutateIncomingTransportMessages> mutators)
        {
            this.mutators = mutators;
        }

        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (hasIncomingTransportMessageMutators)
            {
                return InvokeIncomingTransportMessagesMutators(context, next, cancellationToken);
            }

            return next(context, cancellationToken);
        }

        async Task InvokeIncomingTransportMessagesMutators(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            var mutatorsRegisteredInDI = context.Builder.GetServices<IMutateIncomingTransportMessages>();
            var transportMessage = context.Message;
            var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body, transportMessage.Headers);

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

            await next(context, cancellationToken).ConfigureAwait(false);
        }

        volatile bool hasIncomingTransportMessageMutators = true;
        HashSet<IMutateIncomingTransportMessages> mutators;
    }
}