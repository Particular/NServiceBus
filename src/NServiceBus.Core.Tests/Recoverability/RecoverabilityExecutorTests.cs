namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Pipeline;
using NUnit.Framework;
using OpenTelemetry.Helpers;
using Pipeline;
using Transport;

[TestFixture]
public class RecoverabilityExecutorTests
{
    [Test]
    public async Task Should_share_error_context_extensions()
    {
        var existingValue = Guid.NewGuid();

        ContextBag pipelineExtensions = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                pipelineExtensions = context.Extensions;
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline);

        ErrorContext errorContext = CreateErrorContext();
        errorContext.Extensions.Set("existing value", existingValue);

        await executor.Invoke(errorContext);

        Assert.That(pipelineExtensions.Get<Guid>("existing value"), Is.EqualTo(existingValue));
    }

    [Test]
    public async Task Should_use_error_context_extensions_as_extensions_root()
    {
        var newValue = Guid.NewGuid();

        ContextBag pipelineExtensions = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                context.Extensions.SetOnRoot("new value", newValue);
                pipelineExtensions = context.Extensions;
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline);

        ErrorContext errorContext = CreateErrorContext();

        await executor.Invoke(errorContext);

        Assert.That(errorContext.Extensions.Get<Guid>("new value"), Is.EqualTo(newValue));
    }

    // The transport raises a failure of recoverability as a critical error, which logs it: recoverability does not log
    // it as well.
    [TestCase(ExceptionRecordingMode.Logs)]
    [TestCase(ExceptionRecordingMode.SpanAndLogs)]
    public void Should_record_error_on_the_recoverability_activity_and_not_log_when_pipeline_throws(ExceptionRecordingMode exceptionRecordingMode)
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        Activity pipelineActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = _ =>
            {
                pipelineActivity = Activity.Current;
                throw new InvalidOperationException("dispatch failed");
            }
        };

        var (serviceProvider, entries) = CapturingLoggerProvider.CreateServiceProvider();
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions { ExceptionRecordingMode = exceptionRecordingMode }), serviceProvider);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.Invoke(CreateErrorContext()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pipelineActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(pipelineActivity.IsStopped, Is.True);
            Assert.That(entries.Where(e => e.Exception is InvalidOperationException), Is.Empty);
            string[] expectedEvents = exceptionRecordingMode == ExceptionRecordingMode.SpanAndLogs ? ["exception"] : [];
            Assert.That(pipelineActivity.Events.Select(e => e.Name), Is.EqualTo(expectedEvents));
        }
    }

    [Test]
    public async Task Should_link_recoverability_activity_to_the_failed_processing_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        using var failedActivity = new Activity("handler").Start();
        failedActivity.Stop();

        var errorContext = CreateErrorContext();
        errorContext.Exception.AddActivityContext(failedActivity.Context);

        Activity pipelineActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = _ =>
            {
                pipelineActivity = Activity.Current;
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()));

        await executor.Invoke(errorContext);

        Assert.That(pipelineActivity.Links.Select(l => l.Context), Does.Contain(failedActivity.Context));
    }

    [Test]
    public async Task Should_not_link_recoverability_activity_when_no_failed_processing_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        Activity pipelineActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = _ =>
            {
                pipelineActivity = Activity.Current;
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()));

        await executor.Invoke(CreateErrorContext());

        Assert.That(pipelineActivity.Links, Is.Empty);
    }

    static RecoverabilityPipelineExecutor<object> CreateRecoverabilityExecutor(TestableMessageOperations.Pipeline<IRecoverabilityContext> recoverabilityPipeline, IActivityFactory activityFactory = null, IServiceProvider serviceProvider = null)
    {
        var executor = new RecoverabilityPipelineExecutor<object>(
            serviceProvider ?? new ServiceCollection().AddLogging().BuildServiceProvider(), // TODO: Does not get disposed
            new ThrowingPipelineCache(),
            new TestableMessageOperations(),
            null,
            (_, _) => RecoverabilityAction.Discard("test"),
            recoverabilityPipeline,
            new FaultMetadataExtractor([], _ => { }),
            null,
            activityFactory ?? NoOpActivityFactory.Instance
            );
        return executor;
    }

    static ErrorContext CreateErrorContext() => new(new Exception("test"), [], Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 10, "receive address", new ContextBag());
}