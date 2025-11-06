namespace NServiceBus.Core.Tests.Features;

using System;
using NServiceBus.Features;

public abstract partial class TestFeature : Feature, IFeatureFactory
{
    protected TestFeature() =>
        Defaults(s =>
        {
            OnDefaults?.Invoke(this);
        });

    public Action<Feature> OnActivation;
    public Action<Feature> OnDefaults;

    protected override void Setup(FeatureConfigurationContext context) => OnActivation?.Invoke(this);

    static Feature IFeatureFactory.Create() => throw new NotImplementedException("TestFeature is abstract and should not be instantiated directly");
}
