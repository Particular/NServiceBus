namespace NServiceBus.Core.Tests.Features;

using System;
using NServiceBus.Features;

public abstract partial class TestFeature : Feature
{
    protected TestFeature() =>
        Defaults(s =>
        {
            OnDefaults?.Invoke(this);
        });

    public Action<Feature> OnActivation;
    public Action<Feature> OnDefaults;

    protected override void Setup(FeatureConfigurationContext context) => OnActivation?.Invoke(this);
}
