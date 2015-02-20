namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(context.OutgoingLogicalMessage, context.OutgoingMessage);
            }

            next();
        }
    }
}