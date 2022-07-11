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

        await pipeline.Invoke(new FakeRootContext(), null);

        Assert.IsTrue(invokedPipeline);
    }

    [Test]
    public async Task Invoke_should_set_success_status_when_no_exception()
    {
        var pipeline = new FakePipeline(() => Task.CompletedTask);
        using var activity = new Activity("test activity");
        activity.Start();

        await pipeline.Invoke(new FakeRootContext(), activity);

        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
    }

    [Test]
    public void Invoke_should_set_error_status_and_tags_when_exception()
    {
        var exception = new Exception("test exception");
        var pipeline = new FakePipeline(() => throw exception);
        using var activity = new Activity("test activity");
        activity.Start();

        Assert.ThrowsAsync<Exception>(() => pipeline.Invoke(new FakeRootContext(), activity));

        Assert.AreEqual(ActivityStatusCode.Error, activity.Status);

        var tags = activity.Tags.ToImmutableDictionary();
        Assert.AreEqual("ERROR", tags["otel.status_code"]);
        Assert.AreEqual(exception.Message, tags["otel.status_description"]);

        var errorEvent = activity.Events.Single();
        Assert.AreEqual("exception", errorEvent.Name);
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