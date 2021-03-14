namespace NServiceBus.Features
{
    using MessageMutator;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Threading;

    class Mutators : Feature
    {
        public Mutators()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            var registry = context.Settings.GetOrDefault<RegisteredMutators>() ?? new RegisteredMutators();

            context.Pipeline.Register("MutateIncomingTransportMessage", new MutateIncomingTransportMessageBehavior(registry.IncomingTransportMessage), "Executes IMutateIncomingTransportMessages");
            context.Pipeline.Register("MutateIncomingMessages", new MutateIncomingMessageBehavior(registry.IncomingMessage), "Executes IMutateIncomingMessages");
            context.Pipeline.Register("MutateOutgoingMessages", new MutateOutgoingMessageBehavior(registry.OutgoingMessage), "Executes IMutateOutgoingMessages");
            context.Pipeline.Register("MutateOutgoingTransportMessage", new MutateOutgoingTransportMessageBehavior(registry.OutgoingTransportMessage), "Executes IMutateOutgoingTransportMessages");

            return Task.CompletedTask;
        }

        public class RegisteredMutators
        {
            public readonly HashSet<IMutateIncomingMessages> IncomingMessage = new HashSet<IMutateIncomingMessages>();
            public readonly HashSet<IMutateOutgoingMessages> OutgoingMessage = new HashSet<IMutateOutgoingMessages>();
            public readonly HashSet<IMutateIncomingTransportMessages> IncomingTransportMessage = new HashSet<IMutateIncomingTransportMessages>();
            public readonly HashSet<IMutateOutgoingTransportMessages> OutgoingTransportMessage = new HashSet<IMutateOutgoingTransportMessages>();
        }
    }
}