namespace NServiceBus.MessageMutator
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior<IncomingPhysicalMessageContext>
    {
        public void Invoke(IncomingPhysicalMessageContext context, Action next)
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