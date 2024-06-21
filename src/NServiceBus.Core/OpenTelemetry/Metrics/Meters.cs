namespace NServiceBus;

using System.Diagnostics.Metrics;

class Meters
{
    internal static readonly Meter NServiceBusMeter = new("NServiceBus.Core", "0.2.0");
}