namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var instanceType = context.MessageInstance.GetType();

            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                context.MessageInstance = mutator.MutateOutgoing(context.MessageInstance);
            }

            //if instance type is different we assumes that the user want to change the type
            // this should be made more explicit when we change the mutator api
            if (instanceType != context.MessageInstance.GetType())
            {
                context.MessageType = context.MessageInstance.GetType();
            }

            next();
        }
    }
}