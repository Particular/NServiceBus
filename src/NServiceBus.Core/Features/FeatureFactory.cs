#nullable enable
namespace NServiceBus.Features;

using System;
using System.Diagnostics.CodeAnalysis;

class FeatureFactory
{
    public virtual Feature CreateFeature([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type featureType) => !typeof(Feature).IsAssignableFrom(featureType)
        ? throw new ArgumentException(
            $"The provided type '{featureType.FullName}' is not a valid feature. All features must inherit from '{typeof(Feature).FullName}'.")
        : featureType.Construct<Feature>();

    public virtual Feature CreateFeature<TFeature>() where TFeature : Feature, new() => new TFeature();
}