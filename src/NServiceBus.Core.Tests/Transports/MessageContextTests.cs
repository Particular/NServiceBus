namespace NServiceBus.Core.Tests.Transports;

using System;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class MessageContextTests
{
    [Test]
    public void Should_accept_receive_properties_via_ctor()
    {
        var receiveProperties = new ReceiveProperties
        {
            ["Native.CustomProperty"] = "CustomValue",
            ["AWS.SQS.MessageGroupId"] = "group-123"
        };

        var context = new MessageContext(
            nativeMessageId: "native-id",
            headers: [],
            body: ReadOnlyMemory<byte>.Empty,
            transportTransaction: new TransportTransaction(),
            receiveAddress: "queue@machine",
            context: new ContextBag(),
            receiveProperties: receiveProperties
        );

        var retrieved = context.Extensions.Get<ReceiveProperties>();
        Assert.That(retrieved, Is.SameAs(receiveProperties));
    }

    [Test]
    public void Should_work_without_receive_properties()
    {
        var context = new MessageContext(
            nativeMessageId: "native-id",
            headers: [],
            body: ReadOnlyMemory<byte>.Empty,
            transportTransaction: new TransportTransaction(),
            receiveAddress: "queue@machine",
            context: new ContextBag()
        );

        // Verify backward compatibility: 6-param ctor works, TryGet returns false (not stored)
        Assert.That(context.Extensions.TryGet<ReceiveProperties>(out var props), Is.False);
        Assert.That(props, Is.Null);
    }
}