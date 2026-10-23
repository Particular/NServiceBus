namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Diagnostics;
using Extensibility;
using NUnit.Framework;

[TestFixture]
public class ExceptionTrackingTests
{
    [Test]
    public void Should_share_the_tracking_through_the_root_context_bag()
    {
        var root = new ContextBag();
        var child = new ContextBag(root);

        Assert.That(ExceptionTracking.For(child), Is.SameAs(ExceptionTracking.For(root)));
    }

    [Test]
    public void Should_record_the_exception_event_once_per_exception_instance()
    {
        var tracking = new ExceptionTracking();
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tracking.TryRecordExceptionEvent(first), Is.True);
            Assert.That(tracking.TryRecordExceptionEvent(first), Is.False);
            Assert.That(tracking.TryRecordExceptionEvent(second), Is.True, "a different exception instance is a new exception");
        }
    }

    [Test]
    public void Should_keep_the_first_failed_activity_for_an_exception_instance()
    {
        using var handlerActivity = new Activity("handler").Start();
        using var incomingActivity = new Activity("incoming").Start();
        var tracking = new ExceptionTracking();
        var exception = new InvalidOperationException("boom");

        tracking.RecordFailedActivity(exception, handlerActivity);
        tracking.RecordFailedActivity(exception, incomingActivity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tracking.TryGetFailedActivityContext(exception, out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(handlerActivity.Context));
        }
    }

    [Test]
    public void Should_not_return_the_failed_activity_for_another_exception_instance()
    {
        using var activity = new Activity("handler").Start();
        var tracking = new ExceptionTracking();
        tracking.RecordFailedActivity(new InvalidOperationException("previous attempt"), activity);

        Assert.That(tracking.TryGetFailedActivityContext(new InvalidOperationException("this attempt"), out _), Is.False);
    }
}
