namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using System.Threading;

    class InferredMessageTypeEnricherFeature : Feature
    {
        public InferredMessageTypeEnricherFeature()
        {
            EnableByDefault();
        }

        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            context.Pipeline.Register(typeof(InferredMessageTypeEnricherBehavior), "Adds EnclosedMessageType to the header of the incoming message if it doesn't exist.");
            return Task.CompletedTask;
        }
    }
}