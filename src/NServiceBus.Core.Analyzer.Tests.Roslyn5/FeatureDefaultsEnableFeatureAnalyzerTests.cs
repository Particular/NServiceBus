#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NUnit.Framework;

[TestFixture]
public class FeatureDefaultsEnableFeatureAnalyzerTests : AnalyzerTestFixture<FeatureDefaultsEnableFeatureAnalyzer>
{
    [Test]
    public Task DiagnosticIsReportedForExpressionLambda()
    {
        var source =
            """
            using NServiceBus.Features;

            class SampleFeature : Feature
            {
                public SampleFeature()
                {
                    Defaults(settings => [|settings.EnableFeature<AnotherFeature>()|]);
                }

                protected override void Setup(FeatureConfigurationContext context) { }
            }

            class AnotherFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context) { }
            }
            """;

        return Assert(DiagnosticIds.DoNotEnableFeaturesInDefaults, source);
    }

    [Test]
    public Task DiagnosticIsReportedForMultiple()
    {
        var source =
            """
            using NServiceBus.Features;

            class SampleFeature : Feature
            {
                public SampleFeature()
                {
                    Defaults(settings =>
                    {
                        [|settings.EnableFeature<AnotherFeature>()|];
                        [|settings.EnableFeature<YetAnotherFeature>()|];
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
            """;

        return Assert(DiagnosticIds.DoNotEnableFeaturesInDefaults, source);
    }

    [Test]
    public Task DiagnosticIsReportedForBlockLambda()
    {
        var source =
            """
            using NServiceBus.Features;

            class SampleFeature : Feature
            {
                public SampleFeature()
                {
                    Defaults(settings =>
                    {
                        settings.Set("Key1", 7);
                        [|settings.EnableFeature<AnotherFeature>()|];
                        settings.Set("Key2", 5);
                    });
                }

                protected override void Setup(FeatureConfigurationContext context) { }
            }

            class AnotherFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context) { }
            }
            """;

        return Assert(DiagnosticIds.DoNotEnableFeaturesInDefaults, source);
    }

    [Test]
    public Task DiagnosticIsReportedForMixedMode()
    {
        var source =
            """
            using NServiceBus.Features;

            class SampleFeature : Feature
            {
                public SampleFeature()
                {
                    Enable<AnotherFeature>();
                    Defaults(settings =>
                    {
                        settings.Set("Key1", 7);
                        [|settings.EnableFeature<YetAnotherFeature>()|];
                        settings.Set("Key2", 5);
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
            """;

        return Assert(DiagnosticIds.DoNotEnableFeaturesInDefaults, source);
    }

    [Test]
    public Task DiagnosticIsNotReportedWhenCallingEnable()
    {
        var source =
            """
            using NServiceBus.Features;

            class SampleFeature : Feature
            {
                public SampleFeature()
                {
                    Enable<AnotherFeature>();
                    Defaults(settings => settings.Set("Key", 5));
                }

                protected override void Setup(FeatureConfigurationContext context) { }
            }

            class AnotherFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context) { }
            }
            """;

        return Assert(source);
    }
}
