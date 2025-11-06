namespace NServiceBus.Features;

sealed class InferredMessageTypeEnricherFeature : Feature, IFeatureFactory
{
    protected override void Setup(FeatureConfigurationContext context) => context.Pipeline.Register(
        typeof(InferredMessageTypeEnricherBehavior),
        "Adds EnclosedMessageType to the header of the incoming message if it doesn't exist.");

    static Feature IFeatureFactory.Create() => new InferredMessageTypeEnricherFeature();
}