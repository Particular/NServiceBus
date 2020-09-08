namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MessageMutator;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using Transport;

    class MutateOutgoingTransportMessageBehavior : IBehavior<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>
    {
        public MutateOutgoingTransportMessageBehavior(HashSet<IMutateOutgoingTransportMessages> mutators)
        {
            this.mutators = mutators;
        }

        public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (hasOutgoingTransportMessageMutators)
            {
                return InvokeOutgoingTransportMessageMutators(context, next, cancellationToken);
            }

            return next(context, cancellationToken);
        }

        async Task InvokeOutgoingTransportMessageMutators(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();

            context.Extensions.TryGet(out LogicalMessage incomingLogicalMessage);
            context.Extensions.TryGet(out IncomingMessage incomingPhysicalMessage);

            var mutatorContext = new MutateOutgoingTransportMessageContext(
                context.Body,
                outgoingMessage.Instance,
                context.Headers,
                incomingLogicalMessage?.Instance,
                incomingPhysicalMessage?.Headers);

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

            await next(context, cancellationToken).ConfigureAwait(false);
        }

        volatile bool hasOutgoingTransportMessageMutators = true;
        HashSet<IMutateOutgoingTransportMessages> mutators;
    }
}