namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;
    using Transport;

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
                await mutator.MutateOutgoing(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessage(mutatorContext.OutgoingMessage);
            }

            await next().ConfigureAwait(false);
        }
    }
}