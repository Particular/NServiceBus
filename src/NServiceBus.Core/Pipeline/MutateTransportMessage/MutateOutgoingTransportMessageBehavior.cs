namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;

    class MutateOutgoingTransportMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            //TODO: should not need to do a lookup
            var headers = context.Get<DispatchMessageToTransportConnector.State>()
                .Headers;
            var mutatorContext = new MutateOutgoingTransportMessageContext(context.Body, headers);
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }
            context.Body = mutatorContext.Body;

            next();
        }
    }
}