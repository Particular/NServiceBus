namespace NServiceBus
{
    using System;
    using NServiceBus.MessageMutator;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.TransportDispatch;

    class MutateOutgoingMessageBehavior : Behavior<OutgoingContext>
    {
        public override void Invoke(OutgoingContext context, Action next)
        {
            var instanceType = context.MessageInstance.GetType();

            var mutatorContext = new MutateOutgoingMessagesContext(context.MessageInstance);
            foreach (var mutator in context.Builder.BuildAll<IMutateOutgoingMessages>())
            {
                mutator.MutateOutgoing(mutatorContext);
            }

            if (mutatorContext.MessageInstanceChanged)
            {
                context.MessageInstance = mutatorContext.MessageInstance;

                //if instance type is different we assumes that the user want to change the type
                // this should be made more explicit when we change the mutator api
                if (instanceType != context.MessageInstance.GetType())
                {
                    context.MessageType = context.MessageInstance.GetType();
                }
           
            }
     
            foreach (var header in mutatorContext.Headers)
            {
                context.SetHeader(header.Key,header.Value);
            }

            next();
        }
    }
}