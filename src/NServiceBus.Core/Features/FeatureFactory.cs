#nullable enable
namespace NServiceBus.Features;

using System;

class FeatureFactory
{
    public virtual Feature CreateFeature(Type featureType) => !typeof(Feature).IsAssignableFrom(featureType)
        ? throw new ArgumentException(
            $"The provided type '{featureType.FullName}' is not a valid feature. All features must inherit from '{typeof(Feature).FullName}'.")
        : featureType.Construct<Feature>();
}