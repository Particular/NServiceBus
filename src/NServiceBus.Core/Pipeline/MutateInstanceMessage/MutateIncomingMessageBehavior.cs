namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;

    class MutateIncomingMessageBehavior : LogicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var logicalMessage = context.GetLogicalMessage();
            var current = logicalMessage.Instance;

            var mutatorContext = new MutateIncomingMessageContext(current, context.Headers);
            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                mutator.MutateIncoming(mutatorContext).GetAwaiter().GetResult();
            }

            if (mutatorContext.MessageChanged)
            {
                logicalMessage.UpdateMessageInstance(mutatorContext.Message);
            }
            context.MessageType = logicalMessage.Metadata.MessageType;
            next();
        }
    }
}