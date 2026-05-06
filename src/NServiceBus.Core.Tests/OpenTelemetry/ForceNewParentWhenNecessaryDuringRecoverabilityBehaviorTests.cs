namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class ForceNewParentWhenNecessaryDuringRecoverabilityBehaviorTests
{
    [Test]
    public async Task Should_not_modify_when_trace_not_present()
    {
        var behavior = new ForceNewParentWhenNecessaryDuringRecoverabilityBehavior();

        var context = new TestableRecoverabilityContext();
        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.Not.ContainKey(Headers.StartNewTrace));
        }
    }

    [Test]
    public async Task Should_add_start_new_trace_header_when_trace_present_during_delayed_delivery()
    {
        var behavior = new ForceNewParentWhenNecessaryDuringRecoverabilityBehavior();

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new DelayedRetry(TimeSpan.FromSeconds(10))
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.ContainKey(Headers.StartNewTrace));
        }
    }

    [Test]
    public async Task Should_add_start_new_trace_metadata_when_trace_present_during_move_to_error()
    {
        var behavior = new ForceNewParentWhenNecessaryDuringRecoverabilityBehavior();

        var context = new TestableRecoverabilityContext
        {
            Headers = { { Headers.DiagnosticsTraceParent, "traceparent" } },
            RecoverabilityAction = new MoveToError("errorqueue")
        };

        await behavior.Invoke(context, _ => Task.CompletedTask);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Headers, Does.Not.ContainKey(Headers.StartNewTrace));
            Assert.That(context.Metadata, Does.ContainKey(Headers.StartNewTrace));
        }
    }
}