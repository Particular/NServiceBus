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

    [Test]
    [OpenTelemetryV11Defaults]
    public void Should_seed_every_counter_with_the_endpoint_tags_only()
    {
        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();
        using var meterFactory = new TestMeterFactory();
        _ = new PipelineMetrics(meterFactory, "queue", "disc");

        var counters = listener.metrics.Where(i => i is Counter<long> or UpDownCounter<long>).Select(i => i.Name).ToList();

        Assert.That(counters, Is.Not.Empty);
        Assert.That(listener.ReportedMeters.Keys, Is.EquivalentTo(counters), "only counters are seeded, never histograms");
        using (Assert.EnterMultipleScope())
        {
            foreach (var counter in counters)
            {
                Assert.That(listener.ReportedMeters[counter], Is.Zero, counter);
                Assert.That(listener.Tags[counter].Select(t => t.Key), Is.EquivalentTo(["nservicebus.queue", "nservicebus.discriminator"]), counter);
            }
        }
    }

    // In v11 seeding is the only behavior: delete this test together with the V11BehaviorSwitch block in obsoletes-v10.cs.
    [Test]
    public void Should_not_seed_counters()
    {
        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();
        using var meterFactory = new TestMeterFactory();
        _ = new PipelineMetrics(meterFactory, "queue", "disc");

        Assert.That(listener.ReportedMeters, Is.Empty);
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public void Should_not_seed_counters_on_send_only_endpoints()
    {
        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();
        using var meterFactory = new TestMeterFactory();
        _ = new PipelineMetrics(meterFactory, null, null);

        Assert.That(listener.ReportedMeters, Is.Empty);
    }

    static List<Instrument> CreateInstruments()
    {
        using var meterFactory = new TestMeterFactory();
        _ = new PipelineMetrics(meterFactory, "queue", "disc"); // the constructor creates the instruments

        using var listener = TestingMetricListener.SetupNServiceBusMetricsListener();

        return [.. listener.metrics];
    }
}
