namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Extensibility;
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
    public async Task Should_run_recoverability_pipeline_under_the_ambient_activity()
    {
        using var listener = TestingActivityListener.SetupDiagnosticListener(ActivitySources.Recoverability.Name);
        using var ambientActivity = new Activity("transport receive").Start();

        Activity pipelineActivity = null;
        var recoverabilityPipeline = new TestableMessageOperations.Pipeline<IRecoverabilityContext>
        {
            OnInvoke = _ =>
            {
                pipelineActivity = Activity.Current;
            }
        };

        var executor = CreateRecoverabilityExecutor(recoverabilityPipeline, new ActivityFactory(new InstrumentationOptions()));

        // Starting a new trace clears Activity.Current before the recoverability activity starts, so stopping that
        // activity does not make the ambient activity current again on its own.
        var errorContext = CreateErrorContext(new Dictionary<string, string>
        {
            { Headers.DiagnosticsTraceParent, "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" },
            { Headers.StartNewTrace, bool.TrueString }
        });

        await executor.Invoke(errorContext);

        Assert.That(pipelineActivity, Is.SameAs(ambientActivity));
    }

    static RecoverabilityPipelineExecutor<object> CreateRecoverabilityExecutor(TestableMessageOperations.Pipeline<IRecoverabilityContext> recoverabilityPipeline, IActivityFactory activityFactory = null)
    {
        var executor = new RecoverabilityPipelineExecutor<object>(
            new ServiceCollection().AddLogging().BuildServiceProvider(), // TODO: Does not get disposed
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

    static ErrorContext CreateErrorContext(Dictionary<string, string> headers = null) => new(new Exception("test"), headers ?? [], Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 10, "receive address", new ContextBag());
}