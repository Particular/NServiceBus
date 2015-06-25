namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var mutatorContext = new MutateOutgoingMessagesContext(context.GetMessageInstance());
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.UpdateMessageInstance(mutatorContext.MessageInstance);
            }

            foreach (var header in mutatorContext.Headers)
            {
                context.SetHeader(header.Key, header.Value);
            }

            next();
        }
    }
}