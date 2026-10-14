#nullable enable

namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Helpers;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class RecoverabilityActivityTests
{
    readonly ActivityFactory activityFactory = new(new InstrumentationOptions());

    TestingActivityListener recoverabilityActivityListener;

    [OneTimeSetUp]
    public void Setup() => recoverabilityActivityListener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

    [OneTimeTearDown]
    public void TearDown() => recoverabilityActivityListener.Dispose();

    [Test]
    public void Should_be_child_of_failed_process_activity()
    {
        using var sendActivity = CreateCompletedActivity("send activity");
        using var processActivity = CreateCompletedActivity("process activity");
        var contextBag = new ContextBag();
        contextBag.SetFailedAttempt(processActivity, new Exception("processing failed"));

        using var ambientActivity = new Activity("ambient activity");
        ambientActivity.Start();

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(contextBag, new Dictionary<string, string>
        {
            [Headers.DiagnosticsTraceParent] = sendActivity.Id!
        }));

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity!.ParentId, Is.EqualTo(processActivity.Id), "the failed process activity takes precedence over the sender context and the ambient activity");
            Assert.That(activity.OperationName, Is.EqualTo(ActivityNames.RecoverabilityActivityName));
            Assert.That(activity.Kind, Is.EqualTo(ActivityKind.Internal));
            Assert.That(activity.GetTagItem(ActivityTags.NativeMessageId), Is.Not.Null);
        }
    }

    [Test]
    public void Should_stay_in_trace_of_failed_process_activity_when_start_new_trace_requested()
    {
        using var sendActivity = CreateCompletedActivity("send activity");
        using var processActivity = CreateCompletedActivity("process activity in a new trace");
        var contextBag = new ContextBag();
        contextBag.SetFailedAttempt(processActivity, new Exception("processing failed"));

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(contextBag, new Dictionary<string, string>
        {
            [Headers.DiagnosticsTraceParent] = sendActivity.Id!,
            [Headers.StartNewTrace] = bool.TrueString
        }));

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.TraceId, Is.EqualTo(processActivity.TraceId));
    }

    [Test]
    public void Should_link_to_innermost_failed_activity()
    {
        using var processActivity = CreateCompletedActivity("process activity");
        using var handlerActivity = CreateCompletedActivity("handler activity");
        var exception = new Exception("processing failed");
        var contextBag = new ContextBag();
        contextBag.MarkAsRecordedOnSpan(exception, handlerActivity);
        contextBag.SetFailedAttempt(processActivity, exception);

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(contextBag, []));

        Assert.That(activity, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity!.ParentId, Is.EqualTo(processActivity.Id));
            Assert.That(activity.Links.Select(l => l.Context.SpanId), Is.EqualTo(new[] { handlerActivity.SpanId }));
        }
    }

    [Test]
    public void Should_not_link_when_the_incoming_pipeline_activity_failed_itself()
    {
        using var processActivity = CreateCompletedActivity("process activity");
        var contextBag = new ContextBag();
        contextBag.SetFailedAttempt(processActivity, new Exception("processing failed"));

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(contextBag, []));

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Links, Is.Empty);
    }

    [Test]
    public void Should_ignore_recorded_marker_of_another_exception()
    {
        using var processActivity = CreateCompletedActivity("process activity");
        using var handlerActivity = CreateCompletedActivity("handler activity");
        var contextBag = new ContextBag();
        contextBag.MarkAsRecordedOnSpan(new Exception("earlier failure"), handlerActivity);
        contextBag.SetFailedAttempt(processActivity, new Exception("processing failed"));

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(contextBag, []));

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.Links, Is.Empty);
    }

    [Test]
    public void Should_use_sender_context_when_no_failed_process_activity()
    {
        using var sendActivity = CreateCompletedActivity("send activity");

        using var activity = activityFactory.StartRecoverabilityActivity(CreateErrorContext(new ContextBag(), new Dictionary<string, string>
        {
            [Headers.DiagnosticsTraceParent] = sendActivity.Id!
        }));

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity!.ParentId, Is.EqualTo(sendActivity.Id));
    }

    static Activity CreateCompletedActivity(string activityName)
    {
        var activity = new Activity(activityName);
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        activity.Stop();
        return activity;
    }

    static ErrorContext CreateErrorContext(ContextBag contextBag, Dictionary<string, string> headers) =>
        new(new Exception("processing failed"), headers, Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 1, "receiver", contextBag);
}
