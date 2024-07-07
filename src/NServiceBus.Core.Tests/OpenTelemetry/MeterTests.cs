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
    public void Verify_MeterAPI()
    {
        var meterTags = typeof(MeterTags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
            .Select(x => x.GetRawConstantValue())
            .OrderBy(value => value)
            .ToList();
        using var metricsListener = TestingMetricListener.SetupNServiceBusMetricsListener();
        //The IncomingPipelineMetrics constructor creates the meters, therefore a new instance before collecting the metrics.
        new IncomingPipelineMetrics(new TestMeterFactory(), "queue", "disc");
        var metrics = metricsListener.metrics
            .Select(x => $"{x.Name} => {x.GetType().Name.Split("`").First()}{(x.Unit == null ? "" : ", Unit: ")}{x.Unit ?? ""}")
            .OrderBy(value => value)
            .ToList();
        Approver.Verify(new
        {
            Note = "Changes to metrics API should result in an update to NServiceBusMeter version.",
            ActivitySourceVersion = metricsListener.version,
            Tags = meterTags,
            Metrics = metrics
        });
    }
}