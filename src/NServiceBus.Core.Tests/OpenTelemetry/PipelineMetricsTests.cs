namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using AcceptanceTests.Core.OpenTelemetry.Metrics;
using NUnit.Framework;

[TestFixture]
public class PipelineMetricsTests
{
    // In v11 there is no unadvised variant: delete this test together with the V11BehaviorSwitch block in obsoletes-v10.cs.
    [Test]
    public void Should_not_advise_bucket_boundaries()
    {
        var histograms = CreateInstruments().OfType<Histogram<double>>();

        Assert.That(histograms.Where(h => h.Advice != null).Select(h => h.Name), Is.Empty);
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public void Should_advise_bucket_boundaries_for_every_histogram()
    {
        var histograms = CreateInstruments().OfType<Histogram<double>>().ToList();

        Assert.That(histograms, Is.Not.Empty);
        Assert.That(histograms.Where(h => h.Advice?.HistogramBucketBoundaries == null).Select(h => h.Name), Is.Empty);
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public void Should_declare_a_unit_for_every_instrument()
    {
        var instruments = CreateInstruments();

        Assert.That(instruments, Is.Not.Empty);
        Assert.That(instruments.Where(i => string.IsNullOrEmpty(i.Unit)).Select(i => i.Name), Is.Empty);
    }

    static List<Instrument> CreateInstruments()
    {
        using var meterFactory = new TestMeterFactory();
        _ = new PipelineMetrics(meterFactory, "queue", "disc"); // the constructor creates the instruments

        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();

        return [.. listener.metrics];
    }
}
