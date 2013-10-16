namespace NServiceBus.Pipeline.Behaviors
{
    using MessageMutator;
    using ObjectBuilder;

    class ApplyIncomingTransportMessageMutatorsBehavior : IBehavior
    {
        public IBehavior Next { get; set; }

        public IBuilder Builder { get; set; }

        public void Invoke(BehaviorContext context)
        {
            var mutators = Builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                context.Trace("Applying transport message mutator {0}", mutator);
                mutator.MutateIncoming(context.TransportMessage);
            }

            Next.Invoke(context);
        }
    }
}