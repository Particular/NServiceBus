#nullable enable

namespace NServiceBus.Features;

/// <summary>
/// A root feature that is always enabled.
/// </summary>
sealed class RootFeature : Feature, IFeatureFactory
{
    protected override void Setup(FeatureConfigurationContext context)
    {
    }

    static Feature IFeatureFactory.Create() => new RootFeature();
}