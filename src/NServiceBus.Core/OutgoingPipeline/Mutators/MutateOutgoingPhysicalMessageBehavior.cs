namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.MessageMutator;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.TransportDispatch;

    class MutateOutgoingPhysicalMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var headersSetByMutators = new Dictionary<string, string>();

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                var mutatorContext = new MutateOutgoingTransportMessagesContext(context.Body, headersSetByMutators);

                mutator.MutateOutgoing(mutatorContext);

                context.Body = mutatorContext.Body;
            }

            foreach (var header in headersSetByMutators)
            {
                context.SetHeader(header.Key, header.Value);
            }

            next();
        }
    }
}