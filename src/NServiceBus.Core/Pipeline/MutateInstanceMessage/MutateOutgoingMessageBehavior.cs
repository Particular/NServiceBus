namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using Pipeline;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        public override async Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            IncomingMessage incomingMessage;
            context.Extensions.TryGet(out incomingMessage);
            LogicalMessage logicalMessage;
            context.Extensions.TryGet(out logicalMessage);

            var mutatorContext = new MutateOutgoingMessageContext(
                context.Message.Instance,
                context.Headers,
                logicalMessage?.Instance, 
                incomingMessage?.Headers);

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