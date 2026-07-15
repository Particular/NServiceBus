namespace NServiceBus.Core.Tests.OpenTelemetry;

using NUnit.Framework;

[TestFixture]
public class OpenTelemetryExtensionsTests
{
    [Test]
    public void StartNewTraceOnReceive_should_set_span_link_override_on_send_options()
    {
        var options = new SendOptions();

        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceConnector.SpanLink));
    }

    [Test]
    public void ContinueExistingTraceOnReceive_should_set_child_span_override_on_send_options()
    {
        var options = new SendOptions();

        options.ContinueExistingTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceConnector.ChildSpan));
    }

    [Test]
    public void StartNewTraceOnReceive_should_set_span_link_override_on_publish_options()
    {
        var options = new PublishOptions();

        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceConnector.SpanLink));
    }

    [Test]
    public void ContinueExistingTraceOnReceive_should_set_child_span_override_on_publish_options()
    {
        var options = new PublishOptions();

        options.ContinueExistingTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceConnector.ChildSpan));
    }

    [Test]
    public void Last_override_call_wins()
    {
        var options = new PublishOptions();

        options.ContinueExistingTraceOnReceive();
        options.StartNewTraceOnReceive();

        Assert.That(options.Context.TryGet(OpenTelemetryExtensions.TraceConnectorOverrideKey, out TraceConnector connector), Is.True);
        Assert.That(connector, Is.EqualTo(TraceConnector.SpanLink));
    }
}
