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
    [TestCaseSource(nameof(ActionsThatWriteMetadata))]
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
        }
    }

    [Test]
    public async Task Should_not_write_metadata_for_immediate_retry()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions());

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new ImmediateRetry()
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.Not.ContainKey(Headers.StartNewTrace));
        }
    }

    [Test]
    public async Task Should_always_start_new_trace_for_move_to_error()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions());

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new MoveToError("errorqueue")
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_honor_delayed_retry_trace_mode()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions
        {
            Recoverability = { DelayedRetryTraceMode = TraceMode.ContinueExisting }
        });

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new DelayedRetry(TimeSpan.FromSeconds(10))
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Metadata[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    static IEnumerable<RecoverabilityAction> ActionsThatWriteMetadata()
    {
        yield return new DelayedRetry(TimeSpan.FromSeconds(10));
        yield return new MoveToError("errorqueue");
    }
}