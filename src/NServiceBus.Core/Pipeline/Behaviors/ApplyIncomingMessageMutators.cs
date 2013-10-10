namespace NServiceBus.Pipeline.Behaviors
{
    using MessageMutator;

    public class ApplyIncomingMessageMutators : IBehavior
    {
        readonly IMutateIncomingTransportMessages[] mutators;
        public IBehavior Next { get; set; }

        public ApplyIncomingMessageMutators(IMutateIncomingTransportMessages[] mutators)
        {
            this.mutators = mutators;
        }

        public void Invoke(IBehaviorContext context)
        {
            foreach (var mutator in mutators)
            {
                mutator.MutateIncoming(context.TransportMessage);
            }

            Next.Invoke(context);
        }
    }
}