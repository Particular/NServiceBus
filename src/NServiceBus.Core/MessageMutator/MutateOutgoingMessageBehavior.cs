namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast.Transport;


    class MutateOutgoingMessageBehavior : IBehavior<OutgoingContext>
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