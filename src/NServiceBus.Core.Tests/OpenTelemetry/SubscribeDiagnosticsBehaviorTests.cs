namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class SubscribeDiagnosticsBehaviorTests
{
    const string EventTypesTag = "nservicebus.event_types";

    [Test]
    public async Task Should_tag_event_types_as_comma_separated_string()
    {
        var context = CreateContext(out var activity);

        await new SubscribeDiagnosticsBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(activity.TagObjects.ToImmutableDictionary()[EventTypesTag], Is.EqualTo($"{typeof(FirstEvent)},{typeof(SecondEvent)}"));
    }

    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Should_tag_event_types_as_array_of_full_names()
    {
        var context = CreateContext(out var activity);

        await new SubscribeDiagnosticsBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(activity.TagObjects.ToImmutableDictionary()[EventTypesTag], Is.EqualTo(new[] { typeof(FirstEvent).FullName, typeof(SecondEvent).FullName }));
    }

    static TestableSubscribeContext CreateContext(out Activity activity)
    {
        var context = new TestableSubscribeContext { EventTypes = [typeof(FirstEvent), typeof(SecondEvent)] };

        activity = new Activity("subscribe");
        activity.Start();
        context.Extensions.SetOutgoingPipelineActivity(activity);

        return context;
    }

    class FirstEvent;

    class SecondEvent;
}
