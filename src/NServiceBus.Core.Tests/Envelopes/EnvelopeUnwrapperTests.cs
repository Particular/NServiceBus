namespace NServiceBus.Core.Tests.Envelopes;

using OpenTelemetry;
using System;
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
        originalBody = Encoding.UTF8.GetBytes("payload").AsMemory();
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
        var firstBody = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("firstPayload"));

        var secondHeaders = new Dictionary<string, string>
        {
            ["HeaderC"] = "ValueC"
        };
        var secondBody = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("secondPayload"));
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
        Assert.That(result.Body, Is.EqualTo(firstBody));
    }

    IncomingMessage RunTest() => new EnvelopeUnwrapper(envelopeHandlers.ToArray(), incomingPipelineMetrics).UnwrapEnvelope(messageContext);

    class ReturningHandler(Dictionary<string, string> headersToReturn, ReadOnlyMemory<byte> bodyToReturn) : IEnvelopeHandler
    {
        public (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders,
            ContextBag extensions, ReadOnlyMemory<byte> incomingBody) =>
            (headersToReturn, bodyToReturn);
    }

    class NullReturningHandler : IEnvelopeHandler
    {
        public (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders,
            ContextBag extensions, ReadOnlyMemory<byte> incomingBody) =>
            null;
    }

    class ThrowingHandler : IEnvelopeHandler
    {
        public (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? UnwrapEnvelope(string nativeMessageId, IDictionary<string, string> incomingHeaders,
            ContextBag extensions, ReadOnlyMemory<byte> incomingBody) =>
            throw new InvalidOperationException("Some exception");
    }
}