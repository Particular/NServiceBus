namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Helpers;
using Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Pipeline;

[TestFixture]
public class MessageOperationsTests
{
    TestingActivityListener listener;

    [OneTimeSetUp]
    public void Setup()
    {
        listener = TestingActivityListener.SetupNServiceBusDiagnosticListener();
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        listener?.Dispose();
    }

    // In v11 the status is left unset on success: delete the ActivityStatusCode.Ok assertions in the other tests of
    // this fixture and the attribute on this one.
    [Test]
    [OpenTelemetryV11Defaults]
    public async Task Send_should_leave_status_unset_on_success()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.SendPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Unset));
    }

    [Test]
    public async Task Send_should_create_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.SendPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingMessageActivityName));
            Assert.That(activity.DisplayName, Is.EqualTo("send message"));

            Assert.That(operations.SendPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
        }
    }

    [Test]
    public async Task Publish_should_create_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.PublishPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Publish(new FakeRootContext(), new object(), new PublishOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingEventActivityName));
            Assert.That(activity.DisplayName, Is.EqualTo("publish event"));

            Assert.That(operations.PublishPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
        }
    }

    [Test]
    public async Task Reply_should_create_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.ReplyPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Reply(new FakeRootContext(), new object(), new ReplyOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.OutgoingMessageActivityName));
            Assert.That(activity.DisplayName, Is.EqualTo("reply"));

            Assert.That(operations.ReplyPipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
        }
    }

    [Test]
    public async Task Subscribe_should_create_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.SubscribePipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Subscribe(new FakeRootContext(), typeof(object), new SubscribeOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.SubscribeActivityName));
            Assert.That(activity.DisplayName, Is.EqualTo("subscribe event"));

            Assert.That(operations.SubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
        }
    }

    [Test]
    public async Task Unsubscribe_should_create_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.UnsubscribePipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        await operations.Unsubscribe(new FakeRootContext(), typeof(object), new UnsubscribeOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
            Assert.That(activity.TraceId.ToString(), Is.EqualTo(activity.RootId));

            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.UnsubscribeActivityName));
            Assert.That(activity.DisplayName, Is.EqualTo("unsubscribe event"));

            Assert.That(operations.UnsubscribePipeline.LastContext.Extensions.Get<Activity>(ActivityExtensions.OutgoingActivityKey), Is.EqualTo(activity));
        }
    }

    [Test]
    public void Should_set_span_error_state_on_failure()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.SendPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
            throw new Exception("processing exception");
        };

        var ex = Assert.ThrowsAsync<Exception>(async () => await operations.Send(new FakeRootContext(), new object(), new SendOptions()));

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
        var tags = activity.Tags.ToImmutableDictionary();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tags["otel.status_code"], Is.EqualTo("ERROR"));
            Assert.That(tags["otel.status_description"], Is.EqualTo(ex.Message));
        }
    }

    // No recoverability follows a failed operation on the message session, so the exception is logged right away,
    // whatever the ExceptionRecordingMode, as a warning: the caller gets the exception and decides what it means.
    [TestCase(ExceptionRecordingMode.Logs)]
    [TestCase(ExceptionRecordingMode.SpanAndLogs)]
    public void Should_log_the_exception_as_a_warning_on_failure(ExceptionRecordingMode exceptionRecordingMode)
    {
        var (logger, entries) = CapturingLoggerProvider.CreateLogger<MessageOperations>();
        var operations = new TestableMessageOperations(new ActivityFactory(new InstrumentationOptions { ExceptionRecordingMode = exceptionRecordingMode }), logger);
        var exception = new Exception("processing exception");
        Activity activity = null;
        operations.SendPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
            throw exception;
        };

        Assert.ThrowsAsync<Exception>(async () => await operations.Send(new FakeRootContext(), new object(), new SendOptions()));

        var entry = entries.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Exception, Is.SameAs(exception));
            Assert.That(entry.Message, Is.EqualTo("An exception occurred while executing 'send message'."));
            string[] expectedEvents = exceptionRecordingMode == ExceptionRecordingMode.SpanAndLogs ? ["exception"] : [];
            Assert.That(activity.Events.Select(e => e.Name), Is.EqualTo(expectedEvents));
        }
    }

    // The log entry does not depend on tracing: it is written when no listener created an activity too.
    [Test]
    public void Should_log_the_exception_on_failure_without_an_activity()
    {
        var (logger, entries) = CapturingLoggerProvider.CreateLogger<MessageOperations>();
        var operations = new TestableMessageOperations(NoOpActivityFactory.Instance, logger);
        var exception = new Exception("processing exception");
        operations.SendPipeline.OnInvoke = _ => throw exception;

        Assert.ThrowsAsync<Exception>(async () => await operations.Send(new FakeRootContext(), new object(), new SendOptions()));

        Assert.That(entries.Single().Exception, Is.SameAs(exception));
    }

    [Test]
    public async Task Should_always_create_w3c_id_span()
    {
        var operations = new TestableMessageOperations();
        Activity activity = null;
        operations.SendPipeline.OnInvoke = _ =>
        {
            activity = Activity.Current;
        };

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.SetIdFormat(ActivityIdFormat.Hierarchical);
        ambientActivity.Start();
        Assert.That(ambientActivity.IdFormat, Is.EqualTo(ActivityIdFormat.Hierarchical));

        await operations.Send(new FakeRootContext(), new object(), new SendOptions());

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.IdFormat, Is.EqualTo(ActivityIdFormat.W3C));
            Assert.That(activity.ParentId, Is.EqualTo(ambientActivity.Id));
            Assert.That(activity.TraceId, Is.Not.EqualTo(ambientActivity.TraceId));
        }
    }
}