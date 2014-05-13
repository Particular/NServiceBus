namespace NServiceBus.MessageMutator
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MutateOutgoingPhysicalMessageBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingTransportMessages>())
            {
                mutator.MutateOutgoing(context.OutgoingLogicalMessage, context.OutgoingMessage);
            }

            next();
        }
    }
}