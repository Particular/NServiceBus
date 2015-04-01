namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;

    class ApplyIncomingTransportMessageMutatorsBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingTransportMessages>())
            {
                mutator.MutateIncoming(context.PhysicalMessage);
            }

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingTransportMessage>())
            {
                mutator.MutateIncoming(context.PhysicalMessage, new MutateIncomingTransportMessageContext());
            }

            next();
        }
    }
}