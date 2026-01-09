namespace NServiceBus.Core.Tests.Envelopes;

using OpenTelemetry;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Extensibility;
using NUnit.Framework;
using Transport;


public class EnvelopeUnwrapperTests
{
    string nativeId;
    Dictionary<string, string> originalHeaders;
    ReadOnlyMemory<byte> originalBody;
    MessageContext messageContext;
    TestMeterFactory meterFactory;
    IncomingPipelineMetrics incomingPipelineMetrics;
    List<IEnvelopeHandler> envelopeHandlers;

    [SetUp]
    public void Setup()
    {
        nativeId = "native-1";
        originalHeaders = new()
        {
            ["HeaderA"] = "ValueA"
        };
        originalBody = "payload"u8.ToArray().AsMemory();
        messageContext = new MessageContext(nativeId, originalHeaders, originalBody, new TransportTransaction(), "receiveAddress", new ContextBag());
        meterFactory = new TestMeterFactory();
        incomingPipelineMetrics = new IncomingPipelineMetrics(meterFactory, "queue", "disc");
    }

    [TearDown]
    public void TearDown() => meterFactory.Dispose();

    [Test]
    public void ReturnsDefaultIncomingMessageWhenNoHandlers()
    {
        envelopeHandlers = [];

        IncomingMessage result = RunTest();

        Assert.That(result.NativeMessageId, Is.EqualTo(nativeId));
        Assert.That(result.Headers, Is.EqualTo(originalHeaders));
        Assert.That(result.Body, Is.EqualTo(originalBody));
    }

    [Test]
    public void ReturnsDefaultIncomingMessageWhenHandlersReturnNull()
    {
        envelopeHandlers = [
            new NullReturningHandler(),
            new NullReturningHandler(),
        ];

        IncomingMessage result = RunTest();

        Assert.That(result.NativeMessageId, Is.EqualTo(nativeId));
        Assert.That(result.Headers, Is.EqualTo(originalHeaders));
        Assert.That(result.Body, Is.EqualTo(originalBody));
    }

    [Test]
    public void ReturnsDefaultIncomingMessageWhenHandlersThrow()
    {
        envelopeHandlers = [
            new ThrowingHandler(),
            new ThrowingHandler(),
        ];

        IncomingMessage result = RunTest();

        Assert.That(result.NativeMessageId, Is.EqualTo(nativeId));
        Assert.That(result.Headers, Is.EqualTo(originalHeaders));
        Assert.That(result.Body, Is.EqualTo(originalBody));
    }

    [Test]
    public void ReturnsValueFromTheFirstSucceedingHandler()
    {
        var firstHeaders = new Dictionary<string, string>
        {
            ["HeaderB"] = "ValueB"
        };
        var firstBody = new ReadOnlyMemory<byte>("firstPayload"u8.ToArray());

        var secondHeaders = new Dictionary<string, string>
        {
            ["HeaderC"] = "ValueC"
        };
        var secondBody = new ReadOnlyMemory<byte>("secondPayload"u8.ToArray());
        envelopeHandlers = [
            new NullReturningHandler(),
            new ThrowingHandler(),
            new ReturningHandler(firstHeaders, firstBody),
            new NullReturningHandler(),
            new ThrowingHandler(),
            new ReturningHandler(secondHeaders, secondBody)
        ];

        IncomingMessage result = RunTest();

        Assert.That(result.NativeMessageId, Is.EqualTo(nativeId));
        Assert.That(result.Headers, Is.EqualTo(firstHeaders));
        Assert.That(result.Body.Span.SequenceEqual(firstBody.Span), Is.True);
    }

    [Test]
    public void ModifiedBodyWriterIsReset()
    {
        var firstHeaders = new Dictionary<string, string>
        {
            ["HeaderB"] = "ValueB"
        };
        var firstBody = new ReadOnlyMemory<byte>("firstPayload"u8.ToArray());

        envelopeHandlers = [
            new NullReturningHandler(),
            new ThrowingHandler(),
            new ModifiedBodyWriterResetHandler(),
            new ReturningHandler(firstHeaders, firstBody),
        ];

        IncomingMessage result = RunTest();

        Assert.That(result.NativeMessageId, Is.EqualTo(nativeId));
        Assert.That(result.Headers, Is.EquivalentTo(firstHeaders));
        Assert.That(result.Body.Span.SequenceEqual(firstBody.Span), Is.True);
    }

    EnvelopeUnwrapper.IncomingMessageHandle RunTest() => new EnvelopeUnwrapper([.. envelopeHandlers], incomingPipelineMetrics).UnwrapEnvelope(messageContext);

    class ReturningHandler(Dictionary<string, string> headersToReturn, ReadOnlyMemory<byte> bodyToReturn) : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter)
        {
            bodyWriter.Write(bodyToReturn.Span);
            return headersToReturn;
        }
    }

    class NullReturningHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter) => null;
    }

    class ThrowingHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter) => throw new InvalidOperationException("Some exception");
    }

    class ModifiedBodyWriterResetHandler : IEnvelopeHandler
    {
        public Dictionary<string, string> UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders, ReadOnlySpan<byte> incomingBody, ContextBag extensions, IBufferWriter<byte> bodyWriter)
        {
            bodyWriter.Write("modifiedPayload"u8);
            return null;
        }
    }
}