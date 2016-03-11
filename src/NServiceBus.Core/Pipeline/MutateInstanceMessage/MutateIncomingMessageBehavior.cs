namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;

    class MutateIncomingMessageBehavior : Behavior<IIncomingLogicalMessageContext>
    {
        public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
        {
            var logicalMessage = context.Message;
            var current = logicalMessage.Instance;

            var mutatorContext = new MutateIncomingMessageContext(current, context.Headers);
            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                logicalMessage.UpdateMessageInstance(mutatorContext.Message);
            }

            await next().ConfigureAwait(false);
        }
    }
}