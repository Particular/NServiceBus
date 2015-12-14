namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;
    using OutgoingPipeline;
    using Pipeline;

    class MutateOutgoingTransportMessageBehavior : Behavior<IOutgoingPhysicalMessageContext>
    {
        public override async Task Invoke(IOutgoingPhysicalMessageContext context, Func<Task> next)
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
                await mutator.MutateOutgoing(mutatorContext).ConfigureAwait(false);
            }
            context.Body = mutatorContext.OutgoingBody;

            await next().ConfigureAwait(false);
        }
    }
}