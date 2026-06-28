namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class OpenTelemetrySendBehaviorTests
{
    [Test]
    public async Task Should_set_start_new_trace_to_false_by_default()
    {
        var context = new TestableOutgoingSendContext();

        await new OpenTelemetrySendBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_set_start_new_trace_to_true_when_start_new_trace_requested()
    {
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(OpenTelemetrySendBehavior.StartNewTraceOnReceive, true);

        await new OpenTelemetrySendBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_keep_start_new_trace_false_when_start_new_trace_requested_is_false()
    {
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(OpenTelemetrySendBehavior.StartNewTraceOnReceive, false);

        await new OpenTelemetrySendBehavior().Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }
}
