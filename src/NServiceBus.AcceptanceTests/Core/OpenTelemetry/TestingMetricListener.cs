namespace NServiceBus.AcceptanceTests.Core.OpenTelemetry;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using NUnit.Framework;

class TestingMetricListener : IDisposable
{
    public static TestingMetricListener SetupNServiceBusMetricsListener() =>
        SetupMetricsListener("NServiceBus.Core");

    public static TestingMetricListener SetupMetricsListener(string sourceName)
    {
        var testingMetricListener = new TestingMetricListener(sourceName);
        return testingMetricListener;
    }

    public TestingMetricListener(string sourceName)
    {
        meterListener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == sourceName)
                {
                    TestContext.WriteLine($"Subscribing to {instrument.Meter.Name}\\{instrument.Name}");
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        meterListener.SetMeasurementEventCallback<long>(TrackMeasurement);
        meterListener.SetMeasurementEventCallback<double>(TrackMeasurement);

        meterListener.Start();
    }

    void TrackMeasurement<T>(Instrument instrument,
            T value,
            ReadOnlySpan<KeyValuePair<string, object>> tags,
            object _) where T : struct
    {
        TestContext.WriteLine($"{instrument.Meter.Name}\\{instrument.Name}:{value}");

        var measurement = new Measurement<T>(value, tags);

        ReportedMeters.AddOrUpdate(instrument.Name, (_) => new object[] { measurement }, (_, val) => val.Append(measurement).ToArray());
    }

    public void Dispose() => meterListener?.Dispose();

    public IEnumerable<Measurement<T>> GetReportedMeasurements<T>(string metricName) where T : struct
    {
        if (!ReportedMeters.TryGetValue(metricName, out var measurements))
        {
            yield break;
        }

        foreach (var measurement in measurements)
        {
            yield return (Measurement<T>)measurement;
        }
    }

    public void AssertMetricNotReported(string metricName)
    {
        Assert.False(ReportedMeters.ContainsKey(metricName), $"Should not have '{metricName}' metric reported.");
    }

    ConcurrentDictionary<string, object[]> ReportedMeters { get; } = new();
    readonly MeterListener meterListener;
}