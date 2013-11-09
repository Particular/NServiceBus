namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using MessageMutator;

    class ApplyIncomingMessageMutatorsBehavior : IBehavior<LogicalMessageContext>
    {
        public void Invoke(LogicalMessageContext context, Action next)
        {

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                var current = context.LogicalMessage.Instance;

                //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                ExtensionMethods.CurrentMessageBeingHandled = current;

                context.LogicalMessage.UpdateMessageInstance(mutator.MutateIncoming(current));
            }
            next();
        }
    }
}