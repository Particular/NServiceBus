namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using MessageMutator;

    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior<PhysicalMessageContext>
    {
        public void Invoke(PhysicalMessageContext context, Action next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.PhysicalMessage);
            }

            next();
        }
    }
}