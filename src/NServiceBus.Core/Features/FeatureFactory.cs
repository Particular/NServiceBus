#nullable enable
namespace NServiceBus.Features;

using System;

class FeatureFactory
{
    public virtual Feature CreateFeature(Type featureType)
    {
        if (!typeof(Feature).IsAssignableFrom(featureType))
        {
            throw new ArgumentException(
                $"The provided type '{featureType.FullName}' is not a valid feature. All features must inherit from '{typeof(Feature).FullName}'.");
        }

        if (typeof(IFeatureFactory).IsAssignableFrom(featureType))
        {
            // TODO
            return featureType.Construct<Feature>();
        }
        else
        {
            return featureType.Construct<Feature>();
        }
    }

    public virtual Feature CreateFeature<TFeature>() where TFeature : Feature, IFeatureFactory => TFeature.Create();
}