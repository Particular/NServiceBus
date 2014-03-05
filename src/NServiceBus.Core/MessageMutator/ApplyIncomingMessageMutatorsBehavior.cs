namespace NServiceBus.Pipeline.MessageMutator
{
    using System;
    using System.ComponentModel;
    using Contexts;
    using NServiceBus.MessageMutator;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ApplyIncomingMessageMutatorsBehavior : IBehavior<ReceiveLogicalMessageContext>
    {
        public void Invoke(ReceiveLogicalMessageContext context, Action next)
        {
            var current = context.LogicalMessage.Instance;

            //message mutators may need to assume that this has been set (eg. for the purposes of headers).
            ExtensionMethods.CurrentMessageBeingHandled = current;

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                context.LogicalMessage.UpdateMessageInstance(mutator.MutateIncoming(current));
            }

            next();
        }
    }
}