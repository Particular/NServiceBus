namespace NServiceBus.MessageMutator
{
    using System;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;

    class MutateOutgoingPhysicalMessageBehavior : IBehavior<SendPhysicalMessageContext>
    {
        public void Invoke(SendPhysicalMessageContext context, Action next)
        {
            var messages = context.LogicalMessages.Select(m => m.Instance).ToArray();

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(messages, context.MessageToSend);
            }

            next();
        }
    }
}