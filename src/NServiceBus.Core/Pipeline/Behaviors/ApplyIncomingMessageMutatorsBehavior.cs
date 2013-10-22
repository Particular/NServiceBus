namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using System.Linq;
    using MessageMutator;
    using ObjectBuilder;

    class ApplyIncomingMessageMutatorsBehavior : IBehavior
    {
        public IBuilder Builder { get; set; }

        public void Invoke(BehaviorContext context, Action next)
        {
            context.Messages = context.Messages
                .Select(msg =>
                            {
                                //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                                ExtensionMethods.CurrentMessageBeingHandled = msg;

                                return ApplyIncomingMessageMutatorsTo(msg);
                            })
                .ToArray();

            next();
        }

        object ApplyIncomingMessageMutatorsTo(object originalMessage)
        {
            return Builder.BuildAll<IMutateIncomingMessages>()
                .Aggregate(originalMessage, (current, mutator) => mutator.MutateIncoming(current));
        }
    }
}