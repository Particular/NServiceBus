namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class MutateOutgoingPhysicalMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var headersSetByMutators = new Dictionary<string,string>();

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(new MutateOutgoingTransportMessagesContext(context.Body, headersSetByMutators));
            }

            foreach (var header in headersSetByMutators)
            {
                context.SetHeader(header.Key,header.Value);
            }
            

            next();
        }
    }
}