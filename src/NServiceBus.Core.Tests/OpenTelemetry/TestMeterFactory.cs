namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Generic;
using System.Diagnostics.Metrics;

class TestMeterFactory() : IMeterFactory
{
    List<Meter> meters = [];

    public void Dispose()
    {
        foreach (Meter meter in meters)
        {
            meter.Dispose();
        }
    }

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        meters.Add(meter);
        return meter;
    }
}
