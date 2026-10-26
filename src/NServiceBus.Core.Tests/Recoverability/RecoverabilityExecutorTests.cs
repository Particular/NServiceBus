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
    public async Task Should_link_recoverability_activity_to_the_innermost_failed_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        using var failedActivity = new Activity("handler").Start();
        failedActivity.Stop();

        var errorContext = CreateErrorContext();
        errorContext.Extensions.TryRecordWrappingActivity(errorContext.Exception, failedActivity);

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

        Assert.That(pipelineActivity.Links.Select(l => l.Context), Is.EqualTo(new[] { failedActivity.Context }));
    }

    [Test]
    public async Task Should_log_the_decision_under_the_recoverability_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        Activity recoverabilityActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                recoverabilityActivity = Activity.Current;
            }
        };

        var (serviceProvider, entries) = CapturingLoggerProvider.CreateServiceProvider();
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()), serviceProvider);
        var errorContext = CreateErrorContext();

        await executor.Invoke(errorContext);

        var decisionLog = entries.Single(e => e.Exception == errorContext.Exception);
        Assert.That(decisionLog.Activity, Is.SameAs(recoverabilityActivity));
    }

    [Test]
    public async Task Should_log_only_the_action_that_is_carried_out()
    {
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                context.RecoverabilityAction = RecoverabilityAction.MoveToError("error");
            }
        };

        var (serviceProvider, entries) = CapturingLoggerProvider.CreateServiceProvider();
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, serviceProvider: serviceProvider);

        await executor.Invoke(CreateErrorContext());

        Assert.That(entries.Select(e => e.Category), Is.EqualTo(["NServiceBus.MoveToError"]), "a behavior replaced the policy's decision");
    }

    [Test]
    public async Task Should_log_a_custom_action_as_a_warning()
    {
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                context.RecoverabilityAction = new CustomAction();
            }
        };

        var (serviceProvider, entries) = CapturingLoggerProvider.CreateServiceProvider();
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, serviceProvider: serviceProvider);
        var errorContext = CreateErrorContext();

        await executor.Invoke(errorContext);

        var entry = entries.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Category, Is.EqualTo("NServiceBus.RecoverabilityAction"));
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(entry.Exception, Is.SameAs(errorContext.Exception));
            Assert.That(entry.Message, Does.StartWith($"Recoverability action 'CustomAction' invoked for message '{errorContext.MessageId}'"));
        }
    }

    sealed class CustomAction : RecoverabilityAction
    {
        public override ErrorHandleResult ErrorHandleResult => ErrorHandleResult.Handled;
        public override System.Collections.Generic.IReadOnlyCollection<IRoutingContext> GetRoutingContexts(IRecoverabilityActionContext context) => [];
    }

    [Test]
    public async Task Should_tag_the_recoverability_activity_with_the_action_that_is_carried_out()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        Activity recoverabilityActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = context =>
            {
                recoverabilityActivity = Activity.Current;
                context.RecoverabilityAction = RecoverabilityAction.MoveToError("error");
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()));

        await executor.Invoke(CreateErrorContext());

        Assert.That(recoverabilityActivity.TagObjects.Where(t => t.Key == ActivityTags.RecoverabilityAction).Select(t => t.Value), Is.EqualTo(["move_to_error"]), "a behavior replaced the policy's decision");
    }

    [Test]
    public async Task Should_not_link_recoverability_activity_to_an_activity_that_failed_with_another_exception()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        using var previousAttemptActivity = new Activity("handler").Start();
        previousAttemptActivity.Stop();

        var errorContext = CreateErrorContext();
        errorContext.Extensions.TryRecordWrappingActivity(new Exception("previous attempt"), previousAttemptActivity);

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

        Assert.That(pipelineActivity.Links, Is.Empty);
    }

    [Test]
    public async Task Should_not_link_recoverability_activity_when_no_failed_activity()
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

    [Test]
    public async Task Should_not_fail_when_enriching_the_activity_fails()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>();
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ThrowingEnricherActivityFactory());

        var result = await executor.Invoke(CreateErrorContext());

        Assert.That(result, Is.EqualTo(ErrorHandleResult.Handled));
    }

    [Test]
    public void Should_rethrow_the_pipeline_exception_when_enriching_the_activity_fails()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);

        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = _ => throw new InvalidOperationException("dispatch failed")
        };
        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ThrowingEnricherActivityFactory());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await executor.Invoke(CreateErrorContext()));

        Assert.That(exception.Message, Is.EqualTo("dispatch failed"));
    }

    sealed class ThrowingEnricherActivityFactory : IActivityFactory
    {
        readonly ActivityFactory inner = new(new InstrumentationOptions());

        public InstrumentationOptions Options => inner.Options;
        public Activity StartIncomingPipelineActivity(MessageContext context) => inner.StartIncomingPipelineActivity(context);
        public Activity StartOutgoingPipelineActivity(string activityName, string displayName, IBehaviorContext outgoingContext) => inner.StartOutgoingPipelineActivity(activityName, displayName, outgoingContext);
        public Activity StartHandlerActivity(MessageHandler messageHandler) => inner.StartHandlerActivity(messageHandler);
        public Activity StartRecoverabilityActivity(ErrorContext context) => inner.StartRecoverabilityActivity(context);
        public void EnrichActivityWithRecoverabilityActionData(Activity activity, RecoverabilityAction recoverabilityAction, string receiveAddress) => throw new NotSupportedException("enricher failed");
        public void RecordError(Activity activity, Exception exception, ContextBag contextBag) => inner.RecordError(activity, exception, contextBag);
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