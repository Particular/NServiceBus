namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
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
    public void Should_share_the_innermost_activity_through_the_root_context_bag()
    {
        using var activity = new Activity("send").Start();
        var root = new ContextBag();
        var child = new ContextBag(root);
        var exception = new InvalidOperationException("boom");

        child.TryRecordWrappingActivity(exception, activity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.TryGetWrappingActivityContext(exception, out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(activity.Context));
        }
    }

    [Test]
    public void Should_record_the_innermost_activity_once_per_exception_instance()
    {
        using var sendActivity = new Activity("send").Start();
        using var handlerActivity = new Activity("handler").Start();
        var contextBag = new ContextBag();
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryRecordWrappingActivity(first, sendActivity), Is.True);
            Assert.That(contextBag.TryRecordWrappingActivity(first, handlerActivity), Is.False);
            Assert.That(contextBag.TryRecordWrappingActivity(second, handlerActivity), Is.True, "a different exception instance is a new exception");
        }
    }

    [Test]
    public void Should_keep_the_innermost_activity_for_an_exception_instance()
    {
        using var sendActivity = new Activity("send").Start();
        using var handlerActivity = new Activity("handler").Start();
        var contextBag = new ContextBag();
        var exception = new InvalidOperationException("boom");

        contextBag.TryRecordWrappingActivity(exception, sendActivity);
        contextBag.TryRecordWrappingActivity(exception, handlerActivity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contextBag.TryGetWrappingActivityContext(exception, out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(sendActivity.Context));
        }
    }

    [Test]
    public void Should_not_return_the_innermost_activity_for_another_exception_instance()
    {
        using var activity = new Activity("handler").Start();
        var contextBag = new ContextBag();
        contextBag.TryRecordWrappingActivity(new InvalidOperationException("previous attempt"), activity);

        Assert.That(contextBag.TryGetWrappingActivityContext(new InvalidOperationException("this attempt"), out _), Is.False);
    }
}
