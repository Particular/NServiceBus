namespace NServiceBus.MessageMutator
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;


    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior<IncomingContext>
    {
        public void Invoke(IncomingContext context, Action next)
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