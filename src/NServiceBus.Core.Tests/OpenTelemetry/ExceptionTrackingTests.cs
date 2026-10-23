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
    public void Should_record_the_innermost_activity_once_per_exception_instance()
    {
        using var sendActivity = new Activity("send").Start();
        using var handlerActivity = new Activity("handler").Start();
        var tracking = new ExceptionTracking();
        var first = new InvalidOperationException("first");
        var second = new InvalidOperationException("second");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tracking.TryRecordInnermostActivity(first, sendActivity), Is.True);
            Assert.That(tracking.TryRecordInnermostActivity(first, handlerActivity), Is.False);
            Assert.That(tracking.TryRecordInnermostActivity(second, handlerActivity), Is.True, "a different exception instance is a new exception");
        }
    }

    [Test]
    public void Should_keep_the_innermost_activity_for_an_exception_instance()
    {
        using var sendActivity = new Activity("send").Start();
        using var handlerActivity = new Activity("handler").Start();
        var tracking = new ExceptionTracking();
        var exception = new InvalidOperationException("boom");

        tracking.TryRecordInnermostActivity(exception, sendActivity);
        tracking.TryRecordInnermostActivity(exception, handlerActivity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tracking.TryGetInnermostActivityContext(exception, out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(sendActivity.Context));
        }
    }

    [Test]
    public void Should_not_return_the_innermost_activity_for_another_exception_instance()
    {
        using var activity = new Activity("handler").Start();
        var tracking = new ExceptionTracking();
        tracking.TryRecordInnermostActivity(new InvalidOperationException("previous attempt"), activity);

        Assert.That(tracking.TryGetInnermostActivityContext(new InvalidOperationException("this attempt"), out _), Is.False);
    }
}
