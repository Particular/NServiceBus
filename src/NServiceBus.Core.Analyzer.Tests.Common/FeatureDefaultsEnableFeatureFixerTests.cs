#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NServiceBus.Core.Analyzer.Fixes;
using NUnit.Framework;

[TestFixture]
public class FeatureDefaultsEnableFeatureFixerTests : CodeFixTestFixture<FeatureDefaultsEnableFeatureAnalyzer, FeatureDefaultsEnableFeatureFixer>
{
    [Test]
    public Task ExpressionLambdaIsMovedToConstructor()
    {
        var original =
@"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Defaults(settings => settings.EnableFeature<AnotherFeature>());
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}";

        var expected =
@"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Enable<AnotherFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}";

        return Assert(original, expected);
    }

    [Test]
    public Task MultipleAreMoved()
    {
        var original =
            @"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Defaults(settings => 
        {
            settings.EnableFeature<AnotherFeature>();
            settings.EnableFeature<YetAnotherFeature>();
        });
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}

class YetAnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}
";

        var expected =
            @"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Enable<AnotherFeature>();
        Enable<YetAnotherFeature>();
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}

class YetAnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}
";

        return Assert(original, expected);
    }

    [Test]
    public Task BlockLambdaKeepsRemainingStatements()
    {
        var original =
@"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Defaults(settings =>
        {
            settings.Set(""Key1"", 7);
            settings.EnableFeature<AnotherFeature>();
            settings.Set(""Key2"", 5);
        });
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}";

        var expected =
@"using NServiceBus.Features;

class SampleFeature : Feature
{
    public SampleFeature()
    {
        Enable<AnotherFeature>();
        Defaults(settings =>
        {
            settings.Set(""Key1"", 7);
            settings.Set(""Key2"", 5);
        });
    }

    protected override void Setup(FeatureConfigurationContext context) { }
}

class AnotherFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context) { }
}";

        return Assert(original, expected);
    }
}
