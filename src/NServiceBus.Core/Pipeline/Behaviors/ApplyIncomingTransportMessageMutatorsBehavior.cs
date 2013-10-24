namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using MessageMutator;

    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior
    {
        public void Invoke(BehaviorContext context, Action next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.TransportMessage);
            }

            next();
        }
    }
}