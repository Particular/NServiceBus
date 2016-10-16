namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;
    using Pipeline;

    class MutateIncomingTransportMessageBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();
            var transportMessage = context.Message;
            // Now mutator contains also the zero byte stuff, change?
            var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body ?? transportMessage.BodySegment.Array, transportMessage.Headers);
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
    }
}