namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Transports;
    using Pipeline;

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
                context.UpdateMessageInstance(mutatorContext.OutgoingMessage);
            }

            await next().ConfigureAwait(false);
        }
    }
}