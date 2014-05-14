namespace NServiceBus.MessageMutator
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Transport;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MutateOutgoingMessageBehavior : IBehavior<OutgoingContext>
    {
        public void Invoke(OutgoingContext context, Action next)
        {
            if (context.OutgoingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            var currentMessageToSend = context.OutgoingLogicalMessage.Instance;

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                currentMessageToSend = mutator.MutateOutgoing(currentMessageToSend);
                context.OutgoingLogicalMessage.UpdateMessageInstance(currentMessageToSend);
            }

            next();
        }
    }
}