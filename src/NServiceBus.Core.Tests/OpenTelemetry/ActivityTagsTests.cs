namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class ActivityTagsTests
{
    [Test]
    public void Verify_ActivityTags()
    {
        var activityTags = typeof(ActivityTags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(fi => fi.IsLiteral && !fi.IsInitOnly)
            .Select(x => $"{x.Name} => {x.GetRawConstantValue()}");

        Approver.Verify(new
        {
            Note = "Changes to activity tags should result in ActivitySource version updates",
            Tags = activityTags,
            ActivitySourceVersions = new[]
            {
                new { Name = nameof(ActivitySources.Main), ActivitySources.Main.Version },
                new { Name = nameof(ActivitySources.Handler), ActivitySources.Handler.Version },
                new { Name = nameof(ActivitySources.Recoverability), ActivitySources.Recoverability.Version }
            }});
    }
}