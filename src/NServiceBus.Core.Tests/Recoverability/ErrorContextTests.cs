namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class ErrorContextTests
{
    [Test]
    public void Can_pass_additional_information_via_context_bag()
    {
        var contextBag = new ContextBag();
        contextBag.Set("MyKey", "MyValue");
        var context = new ErrorContext(new Exception(), [], "ID", Array.Empty<byte>(), new TransportTransaction(), 0, "my-queue", contextBag);

        Assert.That(context.Extensions.Get<string>("MyKey"), Is.EqualTo("MyValue"));
    }

    [Test]
    public void Should_carry_receive_properties_on_incoming_message()
    {
        var context = new ContextBag();
        var receiveProperties = new ReceiveProperties(new Dictionary<string, string> { ["Native.Key"] = "Value" });

        var errorContext = new ErrorContext(
            exception: new InvalidOperationException("Test"),
            headers: new Dictionary<string, string> { [Headers.MessageId] = "id" },
            nativeMessageId: "native-id",
            body: ReadOnlyMemory<byte>.Empty,
            receiveProperties: receiveProperties,
            transportTransaction: new TransportTransaction(),
            immediateProcessingFailures: 1,
            receiveAddress: "queue",
            context: context
        );

        using (Assert.EnterMultipleScope())
        {
            Assert.That(errorContext.Message.ReceiveProperties, Is.SameAs(receiveProperties));
            Assert.That(errorContext.Message.ReceiveProperties["Native.Key"], Is.EqualTo("Value"));
        }
    }

    [Test]
    public void Should_default_receive_properties_to_empty_when_not_provided()
    {
        var context = new ContextBag();

        var errorContext = new ErrorContext(
            exception: new InvalidOperationException("Test"),
            headers: [],
            nativeMessageId: "native-id",
            body: ReadOnlyMemory<byte>.Empty,
            transportTransaction: new TransportTransaction(),
            immediateProcessingFailures: 1,
            receiveAddress: "queue",
            context: context
        );

        Assert.That(errorContext.Message.ReceiveProperties, Is.SameAs(ReceiveProperties.Empty));
    }
}