namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class PopulateRecoverabilityTraceMetadataBehaviorTests
{
    [Test]
    public async Task Should_not_write_metadata_when_trace_not_present()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions());

        var context = new TestableRecoverabilityContext();
        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.Not.ContainKey(Headers.StartNewTrace));
        }
    }

    [Test]
    [TestCaseSource(nameof(Actions))]
    public async Task Should_write_metadata_when_trace_present(RecoverabilityAction recoverabilityAction)
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions());

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = recoverabilityAction
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.TrueString), "defaults preserve current behavior");
        }
    }

    [Test]
    public async Task Should_continue_trace_for_delayed_retry_when_connector_is_child_span()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions { DelayedRetryTraceMode = TraceMode.ContinueExisting });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new DelayedRetry(TimeSpan.FromSeconds(10))
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_continue_trace_for_move_to_error_when_connector_is_child_span()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions { ErrorMessageTraceMode = TraceMode.ContinueExisting });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new MoveToError("errorqueue")
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_not_apply_error_connector_to_delayed_retries()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions { ErrorMessageTraceMode = TraceMode.ContinueExisting });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new DelayedRetry(TimeSpan.FromSeconds(10))
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_not_apply_delayed_retry_connector_to_move_to_error()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions { DelayedRetryTraceMode = TraceMode.ContinueExisting });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new MoveToError("errorqueue")
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_start_new_trace_for_other_actions_regardless_of_connectors()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions
        {
            DelayedRetryTraceMode = TraceMode.ContinueExisting,
            ErrorMessageTraceMode = TraceMode.ContinueExisting
        });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new ImmediateRetry()
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    static IEnumerable<RecoverabilityAction> Actions()
    {
        yield return new ImmediateRetry();
        yield return new DelayedRetry(TimeSpan.FromSeconds(10));
        yield return new MoveToError("errorqueue");
    }
}
