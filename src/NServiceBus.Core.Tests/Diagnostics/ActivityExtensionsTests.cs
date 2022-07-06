namespace NServiceBus.Core.Tests.Diagnostics;

using System.Diagnostics;
using Extensibility;
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
        Assert.IsFalse(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity));
        Assert.IsNull(activity);
    }

    [Test]
    public void TryGetRecordingPipelineActivity_should_return_false_when_value_null()
    {
        using var ambientActivity = new Activity("ambient");
        ambientActivity.Start();

        var contextBag = new ContextBag();
        contextBag.SetOutgoingPipelineActitvity(null);

        Assert.IsFalse(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity));
        Assert.IsNull(activity);
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
        contextBag.SetOutgoingPipelineActitvity(recordingActivity);

        Assert.IsTrue(contextBag.TryGetRecordingOutgoingPipelineActivity(out var activity));
        Assert.AreEqual(recordingActivity, activity);
    }
}