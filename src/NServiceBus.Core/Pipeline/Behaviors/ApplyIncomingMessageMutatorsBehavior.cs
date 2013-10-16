namespace NServiceBus.Pipeline.Behaviors
{
    using System.Linq;
    using MessageMutator;
    using ObjectBuilder;

    class ApplyIncomingMessageMutatorsBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IBuilder Builder { get; set; }
    
        public void Invoke(IBehaviorContext context)
        {
            context.Messages = context.Messages
                .Select(msg =>
                            {
                                //message mutators may need to assume that this has been set (eg. for the purposes of headers).
                                ExtensionMethods.CurrentMessageBeingHandled = msg;

                                return ApplyIncomingMessageMutatorsTo(msg);
                            })
                .ToArray();

            Next.Invoke(context);
        }

        object ApplyIncomingMessageMutatorsTo(object originalMessage)
        {
            var mutators = Builder.BuildAll<IMutateIncomingMessages>().ToList();

            var mutatedMessage = originalMessage;
            mutators.ForEach(m =>
                                 {
                                     mutatedMessage = m.MutateIncoming(mutatedMessage);
                                 });

            return mutatedMessage;
        }
    }
}