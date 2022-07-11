namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class MeterTagsTests
{
    [Test]
    public void Verify_MeterTags()
    {
        var meterTags = typeof(MeterTags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
            .Select(x => $"{x.Name} => {x.GetRawConstantValue()}")
            .ToList();

        Approver.Verify(new
        {
            Note = "Changes to meter tags should result in Meters version updates",
            ActivitySourceVersion = Meters.NServiceBusMeter.Version,
            Tags = meterTags
        });
    }
}