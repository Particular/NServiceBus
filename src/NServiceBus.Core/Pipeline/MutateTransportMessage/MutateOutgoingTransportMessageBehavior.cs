namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;
    using Transport;

    class MutateOutgoingTransportMessageBehavior : IBehavior<IOutgoingPhysicalMessageContext, IOutgoingPhysicalMessageContext>
    {
        public MutateOutgoingTransportMessageBehavior(bool hasOutgoingTransportMessageMutators)
        {
            this.hasOutgoingTransportMessageMutators = hasOutgoingTransportMessageMutators;
        }

        public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
        {
            if (hasOutgoingTransportMessageMutators)
            {
                return InvokeOutgoingTransportMessageMutators(context, next);
            }

            return next(context);
        }

        async Task InvokeOutgoingTransportMessageMutators(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
        {
            var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();

            LogicalMessage incomingLogicalMessage;
            context.Extensions.TryGet(out incomingLogicalMessage);

            IncomingMessage incomingPhysicalMessage;
            context.Extensions.TryGet(out incomingPhysicalMessage);

            var mutatorContext = new MutateOutgoingTransportMessageContext(
                context.Body,
                outgoingMessage.Instance,
                context.Headers,
                incomingLogicalMessage?.Instance,
                incomingPhysicalMessage?.Headers);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            if (mutatorContext.MessageBodyChanged)
            {
                context.UpdateMessage(mutatorContext.OutgoingBody);
            }

            await next(context).ConfigureAwait(false);
        }

        bool hasOutgoingTransportMessageMutators;
    }
}