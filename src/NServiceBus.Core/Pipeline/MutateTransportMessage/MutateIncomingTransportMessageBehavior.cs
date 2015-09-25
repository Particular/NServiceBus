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
            var transportMessage = context.Message;
            var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body, transportMessage.Headers);
            foreach (var mutator in mutators)
            {
                await mutator.MutateIncoming(mutatorContext).ConfigureAwait(false);
            }
            transportMessage.Body = mutatorContext.Body;
            await next().ConfigureAwait(false);
        }
    }
}