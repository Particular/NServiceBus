namespace NServiceBus.Core.Tests.OpenTelemetry.Tracing;

using System.Diagnostics;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class SubscribeDiagnosticsBehaviorTests
{
    [Test]
    public async Task Should_set_event_types_tag_when_activity_exists()
    {
        using var ambientActivity = new Activity("ambient").Start();
        using var recordingActivity = new Activity("recording") { ActivityTraceFlags = ActivityTraceFlags.Recorded };
        recordingActivity.Start();

        var context = new TestableSubscribeContext
        {
            EventTypes = [typeof(EventA), typeof(EventB)]
        };
        context.Extensions.SetOutgoingPipelineActivity(recordingActivity);

        await new SubscribeDiagnosticsBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(recordingActivity.GetTagItem(ActivityTags.EventTypes), Is.EqualTo($"{typeof(EventA)},{typeof(EventB)}"));
    }

    [Test]
    public void Should_call_next_when_activity_does_not_exist()
    {
        var context = new TestableSubscribeContext();
        var wasNextCalled = false;

        Assert.DoesNotThrowAsync(async () =>
            await new SubscribeDiagnosticsBehavior().Invoke(context, _ =>
            {
                wasNextCalled = true;
                return Task.CompletedTask;
            }));

        Assert.That(wasNextCalled, Is.True);
    }

    class EventA : IEvent;
    class EventB : IEvent;
}
