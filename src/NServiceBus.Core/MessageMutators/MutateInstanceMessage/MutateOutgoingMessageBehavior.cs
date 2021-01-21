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

    class MutateOutgoingMessageBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public MutateOutgoingMessageBehavior(HashSet<IMutateOutgoingMessages> mutators)
        {
            this.mutators = mutators;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            if (hasOutgoingMessageMutators)
            {
                return InvokeOutgoingMessageMutators(context, next, token);
            }

            return next(context, token);
        }

        async Task InvokeOutgoingMessageMutators(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            context.Extensions.TryGet(out LogicalMessage incomingLogicalMessage);
            context.Extensions.TryGet(out IncomingMessage incomingPhysicalMessage);

            var mutatorContext = new MutateOutgoingMessageContext(
                context.Message.Instance,
                context.Headers,
                incomingLogicalMessage?.Instance,
                incomingPhysicalMessage?.Headers);

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

            await next(context, token).ConfigureAwait(false);
        }

        volatile bool hasOutgoingMessageMutators = true;
        HashSet<IMutateOutgoingMessages> mutators;
    }
}