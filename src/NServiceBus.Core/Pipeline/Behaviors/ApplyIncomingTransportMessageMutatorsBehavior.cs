namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using MessageMutator;
    using ObjectBuilder;

    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior
    {
        public IBuilder Builder { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            var mutators = Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                context.Trace("Applying transport message mutator {0}", mutator);
                mutator.MutateIncoming(context.TransportMessage);
            }

            next();
        }
    }
}