namespace NServiceBus.Features
{
    class Mutators : Feature
    {
        public Mutators()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("MutateIncomingTransportMessage", new MutateIncomingTransportMessageBehavior(), "Executes IMutateIncomingTransportMessages");
            context.Pipeline.Register("MutateIncomingMessages", new MutateIncomingMessageBehavior(), "Executes IMutateIncomingMessages");

            context.Pipeline.Register("MutateOutgoingMessages", new MutateOutgoingMessageBehavior(), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register("MutateOutgoingTransportMessage", new MutateOutgoingTransportMessageBehavior(), "Executes IMutateOutgoingTransportMessages");
        }
    }
}