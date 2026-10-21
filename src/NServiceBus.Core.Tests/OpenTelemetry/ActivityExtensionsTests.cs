namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Diagnostics;
using NServiceBus.Extensibility;
using NUnit.Framework;

[TestFixture]
public class ActivityExtensionsTests
{
    [Test]
    public void TryGetRecordingPipelineActivity_should_return_false_when_key_not_found()
    {
        using var ambientActivity = new Activity("ambient");
        ambientActivity.Start();

        var contextBag = new ContextBag();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryGetOutgoingPipelineActivity(out var activity), Is.False);
            Assert.That(activity, Is.Null);
        }
    }

    [Test]
    public void TryGetRecordingPipelineActivity_should_return_false_when_value_null()
    {
        using var ambientActivity = new Activity("ambient");
        ambientActivity.Start();

        var contextBag = new ContextBag();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryGetOutgoingPipelineActivity(out var activity), Is.False);
            Assert.That(activity, Is.Null);
        }
    }

    [Test]
    public void TryGetRecordingPipelineActivity_should_return_false_when_not_recording()
    {
        using var recordingActivity = new Activity("test activity")
        {
            ActivityTraceFlags = ActivityTraceFlags.Recorded
        };
        recordingActivity.Start();

        var contextBag = new ContextBag();
        contextBag.SetOutgoingPipelineActivity(recordingActivity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryGetOutgoingPipelineActivity(out var activity), Is.True);
            Assert.That(activity, Is.EqualTo(recordingActivity));
        }
    }

    [Test]
    public void SetFailedActivityContext_should_write_the_context_to_the_root()
    {
        using var handlerActivity = new Activity("handler").Start();

        var root = new ContextBag();
        var child = new ContextBag(root);

        child.SetFailedActivityContext(handlerActivity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.TryTakeFailedActivityContext(out var context), Is.True, "the context should be readable from the root bag");
            Assert.That(context, Is.EqualTo(handlerActivity.Context));
        }
    }

    [Test]
    public void TryTakeFailedActivityContext_should_consume_the_context()
    {
        using var activity = new Activity("handler").Start();
        var contextBag = new ContextBag();
        contextBag.SetFailedActivityContext(activity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryTakeFailedActivityContext(out _), Is.True);
            Assert.That(contextBag.TryTakeFailedActivityContext(out _), Is.False);
        }
    }
}