namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            //TODO: should not need to do a lookup
            var headers = context.Get<DispatchMessageToTransportConnector.State>()
                .Headers;
            var mutatorContext = new MutateOutgoingMessageContext(context.GetMessageInstance(), headers);
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.MessageInstance);
            }

            next();
        }
    }
}