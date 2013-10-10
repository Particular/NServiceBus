namespace NServiceBus.Pipeline.Behaviors
{
    using MessageMutator;
    using ObjectBuilder;

    public class ApplyIncomingMessageMutators : IBehavior
    {
        readonly IBuilder builder;
        public IBehavior Next { get; set; }

        public ApplyIncomingMessageMutators(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Invoke(IBehaviorContext context)
        {
            var mutators = builder.BuildAll<IMutateIncomingTransportMessages>();

            foreach (var mutator in mutators)
            {
                context.Trace("Applying transport message mutator {0}", mutator);
                mutator.MutateIncoming(context.TransportMessage);
            }

            Next.Invoke(context);
        }
    }
}