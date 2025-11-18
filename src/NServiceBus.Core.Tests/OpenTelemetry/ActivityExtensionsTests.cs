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
            Assert.That(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity), Is.False);
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
            Assert.That(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity), Is.False);
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
            Assert.That(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity), Is.True);
            Assert.That(activity, Is.EqualTo(recordingActivity));
        }
    }
}