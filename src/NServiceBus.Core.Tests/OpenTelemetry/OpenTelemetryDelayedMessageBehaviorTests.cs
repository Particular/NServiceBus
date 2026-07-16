namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Threading.Tasks;
using DelayedDelivery;
using NServiceBus.Transport;
using NUnit.Framework;
using Testing;

[TestFixture]
public class OpenTelemetryDelayedMessageBehaviorTests
{
    [Test]
    public async Task Should_not_set_context_entry_when_not_delayed()
    {
        var context = new TestableRoutingContext();

        await InvokeBehavior(context, new InstrumentationOptions());

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out _), Is.False);
    }

    [Test]
    public async Task Should_not_set_context_entry_when_dispatch_properties_carry_no_delay()
    {
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties());

        await InvokeBehavior(context, new InstrumentationOptions());

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out _), Is.False);
    }

    [Test]
    public async Task Should_start_new_trace_by_default_when_delayed_with_delay()
    {
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5)) });

        await InvokeBehavior(context, new InstrumentationOptions());

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out var startNewTrace), Is.True);
        Assert.That(startNewTrace, Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_start_new_trace_by_default_when_delayed_with_do_not_deliver_before()
    {
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties { DoNotDeliverBefore = new DoNotDeliverBefore(DateTimeOffset.UtcNow.AddSeconds(5)) });

        await InvokeBehavior(context, new InstrumentationOptions());

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out var startNewTrace), Is.True);
        Assert.That(startNewTrace, Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_continue_trace_when_delayed_connector_is_child_span()
    {
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5)) });

        await InvokeBehavior(context, new InstrumentationOptions { DelayedSendTraceMode = TraceMode.ContinueExisting });

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out var startNewTrace), Is.True);
        Assert.That(startNewTrace, Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_not_set_context_entry_when_per_message_override_present()
    {
        var context = new TestableRoutingContext();
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5)) });
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.ContinueExisting);

        await InvokeBehavior(context, new InstrumentationOptions());

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out _), Is.False,
            "the send behavior already resolved the per-message override into the header");
    }

    [Test]
    public async Task Should_not_set_context_entry_for_delayed_retries()
    {
        var context = new TestableRoutingContext();
        context.Message.Headers[Headers.DelayedRetries] = "1";
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(5)) });

        await InvokeBehavior(context, new InstrumentationOptions { DelayedSendTraceMode = TraceMode.ContinueExisting });

        Assert.That(context.Extensions.TryGet<string>(Headers.StartNewTrace, out _), Is.False,
            "recoverability already decided the trace boundary for delayed retries");
    }

    static Task InvokeBehavior(TestableRoutingContext context, InstrumentationOptions options) =>
        new OpenTelemetryDelayedMessageBehavior(options).Invoke(context, _ => Task.CompletedTask);
}
