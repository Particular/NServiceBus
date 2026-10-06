namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class UnsubscribeDiagnosticsBehaviorTests
{
    const string EventTypesTag = "nservicebus.event_types";

    [Test]
    public async Task Should_tag_event_type_as_string()
    {
        var context = CreateContext(out var activity);

        await new UnsubscribeDiagnosticsBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(activity.TagObjects.ToImmutableDictionary()[EventTypesTag], Is.EqualTo(typeof(DemoEvent).FullName));
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_tag_event_type_as_single_element_array()
    {
        var context = CreateContext(out var activity);

        await new UnsubscribeDiagnosticsBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(activity.TagObjects.ToImmutableDictionary()[EventTypesTag], Is.EqualTo(new[] { typeof(DemoEvent).FullName }));
    }

    static TestableUnsubscribeContext CreateContext(out Activity activity)
    {
        var context = new TestableUnsubscribeContext { EventType = typeof(DemoEvent) };

        activity = new Activity("unsubscribe");
        activity.Start();
        context.Extensions.SetOutgoingPipelineActivity(activity);

        return context;
    }

    class DemoEvent;
}
