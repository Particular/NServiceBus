namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;

    class MutateIncomingMessageBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public MutateIncomingMessageBehavior(bool hasIncomingMessageMutators)
        {
            this.hasIncomingMessageMutators = hasIncomingMessageMutators;
        }

        public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            if (hasIncomingMessageMutators)
            {
                return InvokeIncomingMessageMutators(context, next);
            }

            return next(context);
        }

        async Task InvokeIncomingMessageMutators(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
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
                context.UpdateMessageInstance(mutatorContext.Message);
            }

            await next(context).ConfigureAwait(false);
        }

        bool hasIncomingMessageMutators;
    }
}