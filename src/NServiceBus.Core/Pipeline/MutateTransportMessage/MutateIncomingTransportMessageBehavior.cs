namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using MessageMutator;

    class MutateIncomingTransportMessageBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();
            var incomingMessage = context.Message;
            var mutatorContext = new MutateIncomingTransportMessageContext(incomingMessage.BodyStream, incomingMessage.Headers);
            foreach (var mutator in mutators)
            {
                await mutator.MutateIncoming(mutatorContext).ConfigureAwait(false);
            }
            // TODO: Let's discuss what we do here
            // incomingMessage.BodyStream = mutatorContext.Body;
            await next().ConfigureAwait(false);
        }
    }
}