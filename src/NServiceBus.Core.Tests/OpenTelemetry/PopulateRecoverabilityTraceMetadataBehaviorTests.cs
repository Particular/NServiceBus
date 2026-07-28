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
    public async Task Should_write_metadata_when_trace_present_for_delayed_retry()
    {
        var behavior = new PopulateRecoverabilityTraceMetadataBehavior(new InstrumentationOptions());

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new DelayedRetry(TimeSpan.FromSeconds(10))
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.ContainKey(Headers.StartNewTrace));
        }
    }

    [Test]
    [TestCaseSource(nameof(NonDelayedRetryActions))]
    public async Task Should_not_write_metadata_for_actions_other_than_delayed_retry(RecoverabilityAction recoverabilityAction)
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
            Assert.That(context.Metadata, Does.Not.ContainKey(Headers.StartNewTrace));
        }
    }

    static IEnumerable<RecoverabilityAction> NonDelayedRetryActions()
    {
        yield return new ImmediateRetry();
        yield return new MoveToError("errorqueue");
    }
}