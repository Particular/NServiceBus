namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;


    class ApplyIncomingMessageMutatorsBehavior : LogicalMessageProcessingStageBehavior
    {
        public override Task Invoke(Context context, Func<Task> next)
        {
            var current = context.GetLogicalMessage().Instance;

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                current = mutator.MutateIncoming(current);
                context.GetLogicalMessage().UpdateMessageInstance(current);
            }

            context.MessageType = context.GetLogicalMessage().Metadata.MessageType;
            return next();
        }
    }
}