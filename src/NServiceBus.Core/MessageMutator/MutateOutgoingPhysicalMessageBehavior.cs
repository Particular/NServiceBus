namespace NServiceBus.MessageMutator
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(context.OutgoingLogicalMessage, context.OutgoingMessage);
            }

            next();
        }
    }
}