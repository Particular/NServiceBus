namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;


    class ApplyIncomingMessageMutatorsBehavior : LogicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var current = context.GetLogicalMessage().Instance;

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                current = mutator.MutateIncoming(current);
                context.GetLogicalMessage().UpdateMessageInstance(current);
            }

            context.MessageType = context.GetLogicalMessage().Metadata.MessageType;
            next();
        }
    }
}