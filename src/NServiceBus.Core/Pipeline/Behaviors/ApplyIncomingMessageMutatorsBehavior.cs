namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using MessageMutator;

    class ApplyIncomingMessageMutatorsBehavior : IBehavior
    {
        public void Invoke(BehaviorContext context, Action next)
        {
            foreach (var logicalMessage in context.Get<LogicalMessages>())
            {
               
                foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
                {
                    var current = logicalMessage.Instance;

                    //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                    ExtensionMethods.CurrentMessageBeingHandled = current;

                    logicalMessage.UpdateMessageInstance(mutator.MutateIncoming(current));
                }
            }

            next();
        }
    }
}