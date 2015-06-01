namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : PhysicalOutgoingContextStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var headersSetByMutators = new Dictionary<string,string>();

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingPhysicalMessages>())
            {
                mutator.MutateOutgoing(new MutateOutgoingPhysicalMessageContext(context.Body, headersSetByMutators));
            }

            foreach (var header in headersSetByMutators)
            {
                context.SetHeader(header.Key,header.Value);
            }
            

            next();
        }
    }
}