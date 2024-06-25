namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
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
        var metrics = typeof(Meters)
            .GetFields(BindingFlags.Static | BindingFlags.NonPublic)
            .Where(fi => typeof(Instrument).IsAssignableFrom(fi.FieldType))
            .Select(fi => (Instrument)fi.GetValue(null))
            .Select(x => $"{x.Name} => {x.GetType().Name.Split("`").First()}{(x.Unit == null ? "" : ", Unit: ")}{x.Unit ?? ""}")
            .OrderBy(value => value)
            .ToList();
        Approver.Verify(new
        {
            Note = "Changes to metrics API should result in an update to NServiceBusMeter version.",
            ActivitySourceVersion = Meters.NServiceBusMeter.Version,
            Tags = meterTags,
            Metrics = metrics
        });
    }
}