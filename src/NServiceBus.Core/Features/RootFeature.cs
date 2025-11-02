#nullable enable

namespace NServiceBus.Features;

/// <summary>
/// A root feature that is always enabled.
/// </summary>
sealed class RootFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
    }
}