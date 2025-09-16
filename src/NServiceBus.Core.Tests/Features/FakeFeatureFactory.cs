namespace NServiceBus.Core.Tests.Features;

using System;
using System.Collections.Generic;
using NServiceBus.Features;

class FakeFeatureFactory : FeatureFactory
{
    readonly Dictionary<Type, Feature> features = [];

    public void Add(Feature feature) => features[feature.GetType()] = feature;

    public override Feature CreateFeature(Type featureType) => features[featureType];
}