namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;

    class MutateIncomingMessageBehavior : LogicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            var logicalMessage = context.GetLogicalMessage();
            var current = logicalMessage.Instance;

            var mutatorContext = new MutateIncomingMessageContext(current, context.Headers);
            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                await mutator.MutateIncoming(mutatorContext).ConfigureAwait(false);
            }

            if (mutatorContext.MessageChanged)
            {
                logicalMessage.UpdateMessageInstance(mutatorContext.Message);
            }
            context.MessageType = logicalMessage.Metadata.MessageType;
            await next().ConfigureAwait(false);
        }
    }
}