namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class MutateOutgoingMessageBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
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
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessage(mutatorContext.OutgoingMessage);
            }

            await next().ConfigureAwait(false);
        }
    }
}