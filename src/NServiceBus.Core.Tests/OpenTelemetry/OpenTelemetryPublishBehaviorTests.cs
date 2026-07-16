namespace NServiceBus.Core.Tests.OpenTelemetry;

using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class OpenTelemetryPublishBehaviorTests
{
    [Test]
    public async Task Should_start_new_trace_on_receive_by_default()
    {
        var behavior = new OpenTelemetryPublishBehavior(new InstrumentationOptions());
        var context = new TestableOutgoingPublishContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_continue_trace_on_receive_when_endpoint_connector_is_child_span()
    {
        var behavior = new OpenTelemetryPublishBehavior(new InstrumentationOptions { PublishTraceMode = TraceMode.ContinueExisting });
        var context = new TestableOutgoingPublishContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_prefer_child_span_option_over_endpoint_connector()
    {
        var behavior = new OpenTelemetryPublishBehavior(new InstrumentationOptions { PublishTraceMode = TraceMode.StartNew });
        var context = new TestableOutgoingPublishContext();
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.ContinueExisting);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_prefer_span_link_option_over_endpoint_connector()
    {
        var behavior = new OpenTelemetryPublishBehavior(new InstrumentationOptions { PublishTraceMode = TraceMode.ContinueExisting });
        var context = new TestableOutgoingPublishContext();
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.StartNew);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }
}
