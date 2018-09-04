namespace NServiceBus.Features
{
    using MessageMutator;
    using System.Collections.Generic;

    class Mutators : Feature
    {
        public Mutators()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var registry = context.Settings.GetOrDefault<RegisteredMutators>();

            context.Pipeline.Register("MutateIncomingTransportMessage", b => new MutateIncomingTransportMessageBehavior(registry.IncomingTransportMessage), "Executes IMutateIncomingTransportMessages");
            context.Pipeline.Register("MutateIncomingMessages", new MutateIncomingMessageBehavior(registry.IncomingMessage), "Executes IMutateIncomingMessages");

            context.Pipeline.Register("MutateOutgoingMessages", new MutateOutgoingMessageBehavior(registry.OutgoingMessage), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register("MutateOutgoingTransportMessage", new MutateOutgoingTransportMessageBehavior(registry.OutgoingTransportMessage), "Executes IMutateOutgoingTransportMessages");
        }

        public class RegisteredMutators
        {
            public IList<IMutateIncomingMessages> IncomingMessage = new List<IMutateIncomingMessages>();
            public IList<IMutateOutgoingMessages> OutgoingMessage = new List<IMutateOutgoingMessages>();
            public IList<IMutateIncomingTransportMessages> IncomingTransportMessage = new List<IMutateIncomingTransportMessages>();
            public IList<IMutateOutgoingTransportMessages> OutgoingTransportMessage = new List<IMutateOutgoingTransportMessages>();
        }
    }
}