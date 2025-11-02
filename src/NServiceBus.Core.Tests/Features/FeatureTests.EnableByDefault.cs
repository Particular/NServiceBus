// This file contains tests that use the obsolete Feature.EnableByDefault() method.
// EnableByDefault is obsolete and will be treated as an error in NServiceBus 11.0.
// This file and its tests should be deleted when NServiceBus 11.0 is released.
#pragma warning disable CS0618 // Type or member is obsolete

namespace NServiceBus.Core.Tests.Features;

using NUnit.Framework;

public partial class FeatureTests
{
    [Test]
    public void Should_be_allow_features_to_request_being_enabled_by_default()
    {
        var feature = new MyEnabledByDefaultFeature();

        Assert.That(feature.IsEnabledByDefault, Is.True);
    }

    public class MyEnabledByDefaultFeature : TestFeature
    {
        public MyEnabledByDefaultFeature() => EnableByDefault();
    }
}
