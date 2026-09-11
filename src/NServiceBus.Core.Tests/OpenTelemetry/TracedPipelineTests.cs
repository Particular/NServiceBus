namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class TracedPipelineTests
{
    [Test]
    public async Task Invoke_should_invoke_pipeline_when_activity_null()
    {
        bool invokedPipeline = false;
        var pipeline = new FakePipeline(() =>
        {
            invokedPipeline = true;
            return Task.CompletedTask;
        });

        await Traced(pipeline, null).Invoke(new FakeRootContext());

        Assert.That(invokedPipeline, Is.True);
    }

    [Test]
    public void Invoke_should_rethrow_when_activity_null()
    {
        var exception = new Exception("test exception");
        var pipeline = new FakePipeline(() => throw exception);

        var thrown = Assert.ThrowsAsync<Exception>(() => Traced(pipeline, null).Invoke(new FakeRootContext()));

        Assert.That(thrown, Is.SameAs(exception));
    }

    [Test]
    public async Task Invoke_should_dispose_activity_after_pipeline()
    {
        var activity = new Activity("test activity");
        var pipeline = new FakePipeline(() => Task.CompletedTask);

        await Traced(pipeline, activity).Invoke(new FakeRootContext());

        Assert.That(activity.Duration, Is.Not.EqualTo(TimeSpan.Zero), "activity should have been stopped by disposal");
    }

    [Test]
    public async Task Invoke_should_set_success_status_when_no_exception()
    {
        var pipeline = new FakePipeline(() => Task.CompletedTask);
        using var activity = new Activity("test activity");

        await Traced(pipeline, activity).Invoke(new FakeRootContext());

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
    }

    [Test]
    public void Invoke_should_set_error_status_and_tags_when_exception()
    {
        var exception = new Exception("test exception");
        var pipeline = new FakePipeline(() => throw exception);
        using var activity = new Activity("test activity");

        Assert.ThrowsAsync<Exception>(() => Traced(pipeline, activity).Invoke(new FakeRootContext()));

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));

        var tags = activity.Tags.ToImmutableDictionary();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tags["otel.status_code"], Is.EqualTo("ERROR"));
            Assert.That(tags["otel.status_description"], Is.EqualTo(exception.Message));
        }

        var errorEvent = activity.Events.Single();
        Assert.That(errorEvent.Name, Is.EqualTo("exception"));
    }

    [Test]
    public void Invoke_should_set_error_status_without_exception_event_when_log_mode()
    {
        var exception = new Exception("test exception");
        var pipeline = new FakePipeline(() => throw exception);
        using var activity = new Activity("test activity");

        Assert.ThrowsAsync<Exception>(() => Traced(pipeline, activity, new InstrumentationOptions { ExceptionRecordingMode = ExceptionRecordingMode.Logs }).Invoke(new FakeRootContext()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(activity.Events, Is.Empty, "no exception event should be added when recording via the log instead");
        }
    }

    static TracedPipeline<IBehaviorContext> Traced(IPipeline<IBehaviorContext> pipeline, Activity activity, InstrumentationOptions options = null)
    {
        var factory = new ActivityFactory(options ?? new InstrumentationOptions());
        return new TracedPipeline<IBehaviorContext>(pipeline, factory, (_, _) =>
        {
            activity?.Start();
            return activity;
        });
    }

    class FakePipeline : IPipeline<IBehaviorContext>
    {
        readonly Func<Task> pipelineAction;

#pragma warning disable PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        public FakePipeline(Func<Task> pipelineAction)
#pragma warning restore PS0013 // A Func used as a method parameter with a Task, ValueTask, or ValueTask<T> return type argument should have at least one CancellationToken parameter type argument unless it has a parameter type argument implementing ICancellableContext
        {
            this.pipelineAction = pipelineAction;
        }

        public Task Invoke(IBehaviorContext context) => pipelineAction();
    }
}