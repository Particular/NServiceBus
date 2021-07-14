namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            if (hasIncomingTransportMessageMutators)
            {
                return InvokeIncomingTransportMessagesMutators(context, next);
            }

            return next(context);
        }

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
                context.UpdateMessage(mutatorContext.Body.Bytes);
            }

            await next(context).ConfigureAwait(false);
        }


        volatile bool hasIncomingTransportMessageMutators = true;
        HashSet<IMutateIncomingTransportMessages> mutators;
    }
}