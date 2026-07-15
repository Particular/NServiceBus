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
            Assert.That(options.SentMessageTraceConnector, Is.EqualTo(TraceConnector.ChildSpan), "sends continue the trace by default");
            Assert.That(options.PublishedMessageTraceConnector, Is.EqualTo(TraceConnector.SpanLink), "publishes start a new linked trace by default");
        }
    }
}
