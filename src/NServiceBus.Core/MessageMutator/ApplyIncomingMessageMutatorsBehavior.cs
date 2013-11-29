namespace NServiceBus.Pipeline.MessageMutator
{
    using System;
    using System.ComponentModel;
    using Contexts;
    using NServiceBus.MessageMutator;


    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ApplyIncomingMessageMutatorsBehavior : IBehavior<ReceiveLogicalMessageContext>
    {
        public void Invoke(ReceiveLogicalMessageContext context, Action next)
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