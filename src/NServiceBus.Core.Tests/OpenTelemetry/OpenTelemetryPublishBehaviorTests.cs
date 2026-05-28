namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class OpenTelemetryPublishBehaviorTests
{
    [Test]
    public async Task Should_set_start_new_trace_to_true_by_default()
    {
        var context = new TestableOutgoingPublishContext();

        await new OpenTelemetryPublishBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_set_start_new_trace_to_false_when_continue_trace_requested()
    {
        var context = new TestableOutgoingPublishContext();
        context.Extensions.Set(OpenTelemetryPublishBehavior.ContinueTraceOnReceive, true);

        await new OpenTelemetryPublishBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_keep_start_new_trace_true_when_continue_trace_requested_is_false()
    {
        var context = new TestableOutgoingPublishContext();
        context.Extensions.Set(OpenTelemetryPublishBehavior.ContinueTraceOnReceive, false);

        await new OpenTelemetryPublishBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }
}
