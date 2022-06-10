namespace NServiceBus.AcceptanceTests.Diagnostics
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;

    class TestingMetricListener : IDisposable
    {
        readonly MeterListener meterListener;

        public TestingMetricListener(string sourceName)
        {
            meterListener = new MeterListener();

            meterListener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == sourceName)
                {
                    Console.WriteLine($"Subscribing to {instrument.Meter.Name}\\{instrument.Name}");
                    l.EnableMeasurementEvents(instrument);
                }
            };

            meterListener.SetMeasurementEventCallback((Instrument instrument,
                long measurement,
                ReadOnlySpan<KeyValuePair<string, object>> tags,
                object _) =>
            {
                Console.WriteLine($"{instrument.Meter.Name}\\{instrument.Name}:{measurement}");

                //TODO: Do we need to capture and evaluate tags?
                ReportedMeters.AddOrUpdate(instrument.Name, measurement, (_, val) => val + measurement);
            });
            meterListener.Start();
        }

        public static TestingMetricListener SetupNServiceBusMetricsListener() =>
            SetupMetricsListener("NServiceBus.Diagnostics");

        public static TestingMetricListener SetupMetricsListener(string sourceName)
        {
            var testingMetricListener = new TestingMetricListener(sourceName);
            return testingMetricListener;
        }

        public void Dispose() => meterListener?.Dispose();

        public ConcurrentDictionary<string, long> ReportedMeters { get; } = new();
    }
}