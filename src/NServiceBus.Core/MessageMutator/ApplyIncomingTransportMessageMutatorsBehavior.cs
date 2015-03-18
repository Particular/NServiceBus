namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;


    class ApplyIncomingTransportMessageMutatorsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
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