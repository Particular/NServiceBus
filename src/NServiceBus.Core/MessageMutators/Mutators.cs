namespace NServiceBus.Features;

using System.Collections.Generic;
using MessageMutator;

sealed class Mutators : Feature, IFeatureFactory
{
    protected override void Setup(FeatureConfigurationContext context)
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
        public readonly HashSet<IMutateIncomingMessages> IncomingMessage = [];
        public readonly HashSet<IMutateOutgoingMessages> OutgoingMessage = [];
        public readonly HashSet<IMutateIncomingTransportMessages> IncomingTransportMessage = [];
        public readonly HashSet<IMutateOutgoingTransportMessages> OutgoingTransportMessage = [];
    }

    static Feature IFeatureFactory.Create() => new Mutators();
}