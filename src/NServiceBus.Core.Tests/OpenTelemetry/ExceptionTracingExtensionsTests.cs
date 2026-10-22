namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Diagnostics;
using NUnit.Framework;

[TestFixture]
public class ExceptionTracingExtensionsTests
{
    [Test]
    public void Should_return_the_activity_context_added_to_the_exception()
    {
        using var activity = new Activity("handler").Start();
        var exception = new InvalidOperationException("boom");

        exception.AddActivityContext(activity.Context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.TryGetActivityContext(out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(activity.Context));
        }
    }

    // The innermost activity the exception escapes from adds its context first; the activities further out must
    // not replace it.
    [Test]
    public void Should_keep_the_first_activity_context()
    {
        using var handlerActivity = new Activity("handler").Start();
        using var incomingActivity = new Activity("incoming").Start();
        var exception = new InvalidOperationException("boom");

        exception.AddActivityContext(handlerActivity.Context);
        exception.AddActivityContext(incomingActivity.Context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.TryGetActivityContext(out var activityContext), Is.True);
            Assert.That(activityContext, Is.EqualTo(handlerActivity.Context));
        }
    }

    [Test]
    public void Should_not_return_an_activity_context_when_none_was_added()
    {
        var exception = new InvalidOperationException("boom");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.TryGetActivityContext(out var activityContext), Is.False);
            Assert.That(activityContext, Is.Null);
        }
    }
}
