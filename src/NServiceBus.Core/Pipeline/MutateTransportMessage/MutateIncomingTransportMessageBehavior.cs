namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;

    class MutateIncomingTransportMessageBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();
            var transportMessage = context.GetPhysicalMessage();
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