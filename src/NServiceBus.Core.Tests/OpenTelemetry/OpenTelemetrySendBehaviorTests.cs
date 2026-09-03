namespace NServiceBus.Core.Tests.OpenTelemetry;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;
using DelayedDelivery;
using Transport;

[TestFixture]
public class OpenTelemetrySendBehaviorTests
{
    [Test]
    public async Task Should_continue_trace_on_receive_by_default()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions());
        var context = new TestableOutgoingSendContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }

    [Test]
    public async Task Should_start_new_trace_on_receive_when_endpoint_connector_is_span_link()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions { SendTraceMode = TraceMode.StartNew });
        var context = new TestableOutgoingSendContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_use_existing_trace_on_receive_when_endpoint_trace_mode_is_use_existing()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions { SendTraceMode = TraceMode.UseExisting });
        var context = new TestableOutgoingSendContext();

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo("UseExisting"));
    }

    [Test]
    public async Task Should_prefer_use_existing_option_over_endpoint_connector()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions { SendTraceMode = TraceMode.StartNew });
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.UseExisting);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo("UseExisting"));
    }

    [Test]
    public async Task Should_use_existing_trace_on_receive_for_delayed_message_when_delayed_delivery_trace_mode_is_use_existing()
    {
        var options = new InstrumentationOptions();
        options.DelayedDelivery.SendOperationTraceMode = TraceMode.UseExisting;
        var behavior = new OpenTelemetrySendBehavior(options);
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(10)) });

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo("UseExisting"));
    }

    [Test]
    public async Task Should_use_existing_trace_on_receive_for_saga_timeout_when_saga_timeout_trace_mode_is_use_existing()
    {
        var options = new InstrumentationOptions();
        options.DelayedDelivery.SagaTimeoutTraceMode = TraceMode.UseExisting;
        var behavior = new OpenTelemetrySendBehavior(options);
        var context = new TestableOutgoingSendContext();
        context.Headers[Headers.IsSagaTimeoutMessage] = bool.TrueString;
        context.Extensions.Set(new DispatchProperties { DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromSeconds(10)) });

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo("UseExisting"));
    }

    [Test]
    public async Task Should_prefer_span_link_option_over_endpoint_connector()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions { SendTraceMode = TraceMode.ContinueExisting });
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.StartNew);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.TrueString));
    }

    [Test]
    public async Task Should_prefer_child_span_option_over_endpoint_connector()
    {
        var behavior = new OpenTelemetrySendBehavior(new InstrumentationOptions { SendTraceMode = TraceMode.StartNew });
        var context = new TestableOutgoingSendContext();
        context.Extensions.Set(OpenTelemetryExtensions.TraceConnectorOverrideKey, TraceMode.ContinueExisting);

        await behavior.Invoke(context, _ => Task.CompletedTask);

        Assert.That(context.Headers[Headers.StartNewTrace], Is.EqualTo(bool.FalseString));
    }
}
