namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;

    class MutateIncomingTransportMessageBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public MutateIncomingTransportMessageBehavior(bool hasIncomingTransportMessageMutators)
        {
            this.hasIncomingTransportMessageMutators = hasIncomingTransportMessageMutators;
        }

        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            if (hasIncomingTransportMessageMutators)
            {
                return InvokeIncomingTransportMessagesMutators(context, next);
            }

            return next(context);
        }

        async Task InvokeIncomingTransportMessagesMutators(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();
            var transportMessage = context.Message;
            var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body, transportMessage.Headers);
            foreach (var mutator in mutators)
            {
                await mutator.MutateIncoming(mutatorContext)
                    .ThrowIfNull()
                    .ConfigureAwait(false);
            }

            if (mutatorContext.MessageBodyChanged)
            {
                context.UpdateMessage(mutatorContext.Body);
            }

            await next(context).ConfigureAwait(false);
        }

        bool hasIncomingTransportMessageMutators;
    }
}