namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using Extensibility;
using NUnit.Framework;
using Transport;

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
    public void Should_propagate_receive_properties_from_context_bag()
    {
        var context = new ContextBag();
        var receiveProperties = new ReceiveProperties { ["Native.Key"] = "Value" };
        context.Set(receiveProperties);

        var errorContext = new ErrorContext(
            exception: new InvalidOperationException("Test"),
            headers: new Dictionary<string, string> { [Headers.MessageId] = "id" },
            nativeMessageId: "native-id",
            body: ReadOnlyMemory<byte>.Empty,
            transportTransaction: new TransportTransaction(),
            immediateProcessingFailures: 1,
            receiveAddress: "queue",
            context: context
        );

        var retrieved = errorContext.Extensions.Get<ReceiveProperties>();
        Assert.That(retrieved, Is.SameAs(receiveProperties));
    }
}