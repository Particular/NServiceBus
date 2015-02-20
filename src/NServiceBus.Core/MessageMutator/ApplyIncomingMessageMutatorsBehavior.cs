namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline.Contexts;


    class ApplyIncomingMessageMutatorsBehavior : LogicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            var current = context.IncomingLogicalMessage.Instance;

            foreach (var mutator in context.Builder.BuildAll<IMutateIncomingMessages>())
            {
                current = mutator.MutateIncoming(current);
                context.IncomingLogicalMessage.UpdateMessageInstance(current);
            }
          
            //we'll soon remove this when we add SendOptions
            ExtensionMethods.CurrentMessageBeingHandled = current;
          
            next();
        }
    }
}