namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry.Metrics;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using NUnit.Framework;

class TestingMetricListener : IDisposable
{
    readonly MeterListener meterListener;
    public readonly List<Instrument> metrics = [];
    public string version = "";
    public string metricsSourceName = "";

    TestingMetricListener(string sourceName)
    {
        meterListener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name != sourceName)
                {
                    return;
                }

                TestContext.Out.WriteLine($"Subscribing to {instrument.Meter.Name}\\{instrument.Name}");
                listener.EnableMeasurementEvents(instrument);
                metrics.Add(instrument);

                version = instrument.Meter.Version;
                metricsSourceName = instrument.Meter.Name;
            }
        };

        // Counters measure in long, the histograms in the incoming pipeline meter measure in double. Both callbacks
        // have to be registered or the measurements (and therefore the tags) of one kind are never observed. A
        // counter reports the measurement itself, a histogram reports how many times it recorded.
        meterListener.SetMeasurementEventCallback((Instrument instrument,
            long measurement,
            ReadOnlySpan<KeyValuePair<string, object>> tags,
            object _) => RecordMeasurement(instrument, measurement, measurement, tags));

        meterListener.SetMeasurementEventCallback((Instrument instrument,
            double measurement,
            ReadOnlySpan<KeyValuePair<string, object>> tags,
            object _) => RecordMeasurement(instrument, measurement, 1, tags));

        meterListener.Start();
    }

    public static TestingMetricListener SetupNServiceBusMetricsListener() =>
        SetupMetricsListener("NServiceBus.Core.Pipeline.Incoming");

    public static TestingMetricListener SetupMetricsListener(string sourceName)
    {
        var testingMetricListener = new TestingMetricListener(sourceName);
        return testingMetricListener;
    }

    public void Dispose() => meterListener?.Dispose();

    void RecordMeasurement<T>(Instrument instrument, T measurement, long reportedValue, ReadOnlySpan<KeyValuePair<string, object>> measurementTags)
    {
        TestContext.Out.WriteLine($"{instrument.Meter.Name}\\{instrument.Name}:{measurement}");

        var tags = measurementTags.ToArray();
        ReportedMeters.AddOrUpdate(instrument.Name, reportedValue, (_, val) => val + reportedValue);
        Tags.AddOrUpdate(instrument.Name, _ => tags, (_, _) => tags);
    }

    public ConcurrentDictionary<string, long> ReportedMeters { get; } = new();
    public ConcurrentDictionary<string, KeyValuePair<string, object>[]> Tags { get; } = new();

    public void AssertMetric(string metricName, long expected)
    {
        if (expected == 0)
        {
            Assert.That(ReportedMeters.ContainsKey(metricName), Is.False, $"Should not have '{metricName}' metric reported.");
        }
        else
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReportedMeters.ContainsKey(metricName), Is.True, $"'{metricName}' metric was not reported.");
                Assert.That(ReportedMeters[metricName], Is.EqualTo(expected));
            }
        }
    }

    public object AssertTagKeyExists(string metricName, string tagKey)
    {
        if (!Tags.ContainsKey(metricName))
        {
            Assert.Fail($"'{metricName}' metric was not reported");
        }

        var emptyTag = default(KeyValuePair<string, object>);
        var meterTag = Tags[metricName].FirstOrDefault(t => t.Key == tagKey);
        if (meterTag.Equals(emptyTag))
        {
            Assert.Fail($"'{tagKey}' tag was not found.");
        }

        return meterTag.Value;
    }
}