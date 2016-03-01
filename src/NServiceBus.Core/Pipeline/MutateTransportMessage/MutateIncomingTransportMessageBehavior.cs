namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;

    class MutateIncomingTransportMessageBehavior : Behavior<IIncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();
            var mutatorContext = new MutateIncomingTransportMessageContext(context.Body, context.Headers);
            foreach (var mutator in mutators)
            {
                await mutator.MutateIncoming(mutatorContext).ConfigureAwait(false);
            }

            if (mutatorContext.MessageBodyChanged)
            {
                context.UpdateMessage(mutatorContext.Body);
            }

            await next().ConfigureAwait(false);
        }
    }
}