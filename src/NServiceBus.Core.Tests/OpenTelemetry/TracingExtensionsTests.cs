namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class TracingExtensionsTests
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

        await pipeline.Invoke(new FakeRootContext(), null, new ActivityFactory(new InstrumentationOptions()));

        Assert.That(invokedPipeline, Is.True);
    }

    [Test]
    public async Task Invoke_should_set_success_status_when_no_exception()
    {
        var pipeline = new FakePipeline(() => Task.CompletedTask);
        using var activity = new Activity("test activity");
        activity.Start();

        await pipeline.Invoke(new FakeRootContext(), activity, new ActivityFactory(new InstrumentationOptions()));

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
    }

    [Test]
    public void Invoke_should_set_error_status_and_tags_when_exception()
    {
        var exception = new Exception("test exception");
        var pipeline = new FakePipeline(() => throw exception);
        using var activity = new Activity("test activity");
        activity.Start();

        Assert.ThrowsAsync<Exception>(() => pipeline.Invoke(new FakeRootContext(), activity, new ActivityFactory(new InstrumentationOptions())));

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
        activity.Start();

        Assert.ThrowsAsync<Exception>(() => pipeline.Invoke(new FakeRootContext(), activity, new ActivityFactory(new InstrumentationOptions { ExceptionRecordingMode = ExceptionRecordingMode.Logs })));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(activity.Events, Is.Empty, "no exception event should be added when recording via the log instead");
        }
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