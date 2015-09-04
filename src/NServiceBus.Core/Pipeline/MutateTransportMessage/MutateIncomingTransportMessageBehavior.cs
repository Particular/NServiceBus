namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;


    class MutateIncomingTransportMessageBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var mutators = context.Builder.BuildAll<IMutateIncomingTransportMessages>();

            var transportMessage = context.GetPhysicalMessage();
            var mutatorContext = new MutateIncomingTransportMessageContext(transportMessage.Body, transportMessage.Headers);
            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(mutatorContext);
            }
            transportMessage.Body = mutatorContext.Body;
            next();
        }
    }
}