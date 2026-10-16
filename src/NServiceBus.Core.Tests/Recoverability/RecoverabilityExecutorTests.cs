namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Extensibility;
using Logging;
using Microsoft.Extensions.Logging;
using NServiceBus.Pipeline;
using OpenTelemetry.Helpers;
using Transport;
using NUnit.Framework;

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

    [Test]
    public void Should_record_error_on_the_recoverability_activity_when_pipeline_throws()
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
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()), serviceProvider);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.Invoke(CreateErrorContext()));

        // Nothing follows a failure of recoverability itself, so the exception is logged right away.
        var exceptionLog = entries.Single(e => e.Exception is InvalidOperationException);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(pipelineActivity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(pipelineActivity.IsStopped, Is.True);
            Assert.That(exceptionLog.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(exceptionLog.Message, Is.EqualTo($"An exception occurred while executing '{pipelineActivity.DisplayName}'."));
        }
    }

    [Test]
    public async Task Should_link_recoverability_activity_to_the_failed_processing_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        using var failedActivity = new Activity("handler").Start();
        failedActivity.Stop();

        var errorContext = CreateErrorContext();
        errorContext.Extensions.SetFailedActivityContext(failedActivity);

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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pipelineActivity.Links.Select(l => l.Context), Does.Contain(failedActivity.Context));
            Assert.That(errorContext.Extensions.TryTakeFailedActivityContext(out _), Is.False, "the failed activity context should be consumed");
        }
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