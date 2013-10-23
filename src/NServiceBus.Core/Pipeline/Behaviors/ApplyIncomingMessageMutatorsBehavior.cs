namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using MessageMutator;

    class ApplyIncomingMessageMutatorsBehavior : IBehavior
    {
        public void Invoke(BehaviorContext context, Action next)
        {
            context.Messages = context.Messages
                .Select(msg =>
                            {
                                //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                                ExtensionMethods.CurrentMessageBeingHandled = msg;

                                return context.Builder.BuildAll<IMutateIncomingMessages>()
                                    .Aggregate(msg, (current, mutator) => mutator.MutateIncoming(current));
                            })
                .ToArray();

            next();
        }
    }
}