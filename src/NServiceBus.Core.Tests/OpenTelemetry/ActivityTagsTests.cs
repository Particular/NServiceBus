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
            // All sources share one version. The constants are listed instead of ActivitySource.Version because the
            // sources pick their version from the V11 switch once per process, see ActivitySources.cs.
            ActivitySourceVersion = new
            {
                ActivitySources.PreV11Version,
                ActivitySources.Version
            }
        });
    }
}