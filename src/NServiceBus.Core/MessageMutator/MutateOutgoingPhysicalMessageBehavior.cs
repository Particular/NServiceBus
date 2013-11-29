namespace NServiceBus.MessageMutator
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Pipeline;
    using Pipeline.Contexts;


    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MutateOutgoingPhysicalMessageBehavior : IBehavior<SendPhysicalMessageContext>
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