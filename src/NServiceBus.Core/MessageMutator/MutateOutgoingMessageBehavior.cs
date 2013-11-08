namespace NServiceBus.MessageMutator
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    internal class MutateOutgoingMessageBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            foreach (var mutator in  context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                context.MessageToSend.UpdateMessageInstance(mutator.MutateOutgoing(context.MessageToSend.Instance));
            }

            next();
        }
    }
}