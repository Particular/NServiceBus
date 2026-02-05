#nullable enable
namespace NServiceBus.Features;

using System;
using System.Diagnostics.CodeAnalysis;

class FeatureFactory
{
    public virtual Feature CreateFeature([DynamicallyAccessedMembers(DynamicMemberTypeAccess.Feature)] Type featureType) => !typeof(Feature).IsAssignableFrom(featureType)
        ? throw new ArgumentException(
            $"The provided type '{featureType.FullName}' is not a valid feature. All features must inherit from '{typeof(Feature).FullName}'.")
        : (Feature)Activator.CreateInstance(featureType, nonPublic: true)!;

    public virtual Feature CreateFeature<TFeature>() where TFeature : Feature, new() => new TFeature();
}