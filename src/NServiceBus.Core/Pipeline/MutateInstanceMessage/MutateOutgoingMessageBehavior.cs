namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;
    using Transport;

    class MutateOutgoingMessageBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public MutateOutgoingMessageBehavior(bool hasOutgoingMessageMutators)
        {
            this.hasOutgoingMessageMutators = hasOutgoingMessageMutators;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            if (hasOutgoingMessageMutators)
            {
                return InvokeOutgoingMessageMutators(context, next);
            }

            return next(context);
        }

        async Task InvokeOutgoingMessageMutators(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            LogicalMessage incomingLogicalMessage;
            context.Extensions.TryGet(out incomingLogicalMessage);

            IncomingMessage incomingPhysicalMessage;
            context.Extensions.TryGet(out incomingPhysicalMessage);

            var mutatorContext = new MutateOutgoingMessageContext(
                context.Message.Instance,
                context.Headers,
                incomingLogicalMessage?.Instance,
                incomingPhysicalMessage?.Headers);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessage(mutatorContext.OutgoingMessage);
            }

            await next(context).ConfigureAwait(false);
        }

        bool hasOutgoingMessageMutators;
    }
}