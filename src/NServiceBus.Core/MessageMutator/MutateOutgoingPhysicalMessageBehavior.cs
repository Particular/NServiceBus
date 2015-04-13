namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingPhysicalContext>())
            {
                mutator.MutateOutgoing(new OutgoingPhysicalMutatorContext(context.Body,context.Headers));
            }

            next();
        }
    }
}