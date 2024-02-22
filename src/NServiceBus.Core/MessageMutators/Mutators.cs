namespace NServiceBus.Features
{
    using System.Collections.Generic;
    using MessageMutator;

    class Mutators : Feature
    {
        public Mutators()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var registry = context.Settings.GetOrDefault<RegisteredMutators>() ?? new RegisteredMutators();

            context.Pipeline.Register("MutateIncomingTransportMessage", new MutateIncomingTransportMessageBehavior(registry.IncomingTransportMessage), "Executes IMutateIncomingTransportMessages");
            context.Pipeline.Register("MutateIncomingMessages", new MutateIncomingMessageBehavior(registry.IncomingMessage), "Executes IMutateIncomingMessages");
            context.Pipeline.Register("MutateOutgoingMessages", new MutateOutgoingMessageBehavior(registry.OutgoingMessage), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register("MutateOutgoingTransportMessage", new MutateOutgoingTransportMessageBehavior(registry.OutgoingTransportMessage), "Executes IMutateOutgoingTransportMessages");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0025:Dictionary keys should implement GetHashCode", Justification = "Mutators are registered based on reference equality")]
        public class RegisteredMutators
        {
            public readonly HashSet<IMutateIncomingMessages> IncomingMessage = new HashSet<IMutateIncomingMessages>();
            public readonly HashSet<IMutateOutgoingMessages> OutgoingMessage = new HashSet<IMutateOutgoingMessages>();
            public readonly HashSet<IMutateIncomingTransportMessages> IncomingTransportMessage = new HashSet<IMutateIncomingTransportMessages>();
            public readonly HashSet<IMutateOutgoingTransportMessages> OutgoingTransportMessage = new HashSet<IMutateOutgoingTransportMessages>();
        }
    }
}