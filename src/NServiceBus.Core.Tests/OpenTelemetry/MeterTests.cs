namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Linq;
using System.Reflection;
using AcceptanceTests.Core.OpenTelemetry.Metrics;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class MeterTests
{
    [Test]
    public void Verify_MeterAPI() => Approver.Verify(CaptureMeterApi());

    // In v11 the renamed meter is the only one: delete Verify_MeterAPI and its approval file, then rename this
    // test and its approval file to Verify_MeterAPI.
    [Test]
    [OpenTelemetryV11Defaults]
    public void Verify_MeterAPI_v11() => Approver.Verify(CaptureMeterApi());

    static object CaptureMeterApi()
    {
        var meterTags = typeof(MeterTags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
            .Select(x => x.GetRawConstantValue())
            .OrderBy(value => value)
            .ToList();

        using var meterFactory = new TestMeterFactory();
        //The IncomingPipelineMeter constructor creates the meters, therefore a new instance before collecting the metrics.
#pragma warning disable CA1806
        new PipelineMetrics(meterFactory, "queue", "disc");
#pragma warning restore CA1806

        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();

        var metrics = metricsListener.metrics
            .Select(x => $"{x.Name} => {x.GetType().Name.Split("`").First()}{(x.Unit == null ? "" : ", Unit: ")}{x.Unit ?? ""}")
            .OrderBy(value => value)
            .ToList();
        return new
        {
            Note = "Changes to metrics API should result in an update to NServiceBusMeter version.",
            MetricsSourceName = metricsListener.metricsSourceName,
            MetricsSourceVersion = metricsListener.version,
            Tags = meterTags,
            Metrics = metrics
        };
    }
}