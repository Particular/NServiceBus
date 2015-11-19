namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using OutgoingPipeline;
    using Pipeline;

    class MutateOutgoingTransportMessageBehavior : Behavior<OutgoingPhysicalMessageContext>
    {
        public override async Task Invoke(OutgoingPhysicalMessageContext context, Func<Task> next)
        {
            var outgoingMessage = context.Extensions.Get<OutgoingLogicalMessage>();

            IncomingMessage incomingMessage;
            context.Extensions.TryGet(out incomingMessage);
            LogicalMessage logicalMessage;
            context.Extensions.TryGet(out logicalMessage);

            var mutatorContext = new MutateOutgoingTransportMessageContext(
                context.Body, 
                outgoingMessage.Instance, 
                context.Headers,
                logicalMessage?.Instance,
                incomingMessage?.Headers);

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }
            context.Body = mutatorContext.OutgoingBody;

            await next().ConfigureAwait(false);
        }
    }
}