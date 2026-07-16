namespace NServiceBus.Core.Tests.OpenTelemetry;

using NUnit.Framework;

[TestFixture]
public class InstrumentationOptionsTests
{
    [Test]
    public void Should_default_trace_connectors_to_current_behavior()
    {
        var options = new InstrumentationOptions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.SendTraceMode, Is.EqualTo(TraceMode.ContinueExisting), "sends continue the trace by default");
            Assert.That(options.PublishTraceMode, Is.EqualTo(TraceMode.StartNew), "publishes start a new linked trace by default");
        }
    }

    [Test]
    public void Should_default_delayed_and_error_trace_connectors_to_span_link()
    {
        var options = new InstrumentationOptions();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.DelayedSendTraceMode, Is.EqualTo(TraceMode.StartNew), "delayed sends start a new linked trace by default");
            Assert.That(options.DelayedRetryTraceMode, Is.EqualTo(TraceMode.StartNew), "delayed retries start a new linked trace by default");
            Assert.That(options.ErrorMessageTraceMode, Is.EqualTo(TraceMode.StartNew), "messages moved to the error queue start a new linked trace by default");
        }
    }
}
