namespace NServiceBus.Pipeline
{
    using System;
    using System.Linq;
    using MessageMutator;

    internal class MutateOutgoingPhysicalMessageBehavior : IBehavior<SendPhysicalMessageContext>
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