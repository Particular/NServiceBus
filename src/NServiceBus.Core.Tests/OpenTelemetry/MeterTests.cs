namespace NServiceBus.Core.Tests.OpenTelemetry;

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
            .Select(x => $"{x.Name} => {x.GetRawConstantValue()}")
            .ToList();
        var metrics = typeof(Metrics)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
            .Select(x => $"{x.Name} => {x.GetRawConstantValue()}")
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