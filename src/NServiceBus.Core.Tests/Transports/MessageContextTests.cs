namespace NServiceBus.Core.Tests.Transports;

using System;
using System.Collections.Generic;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class MessageContextTests
{
    [Test]
    public void Should_accept_receive_properties_via_ctor()
    {
        var receiveProperties = new ReceiveProperties(new Dictionary<string, string>
        {
            ["Native.CustomProperty"] = "CustomValue",
            ["AWS.SQS.MessageGroupId"] = "group-123"
        });

        var context = new MessageContext(
            nativeMessageId: "native-id",
            headers: [],
            body: ReadOnlyMemory<byte>.Empty,
            receiveProperties: receiveProperties,
            transportTransaction: new TransportTransaction(),
            receiveAddress: "queue@machine",
            context: new ContextBag()
        );

        Assert.That(context.ReceiveProperties, Is.SameAs(receiveProperties));
    }

    [Test]
    public void Should_default_receive_properties_to_empty()
    {
        var context = new MessageContext(
            nativeMessageId: "native-id",
            headers: [],
            body: ReadOnlyMemory<byte>.Empty,
            transportTransaction: new TransportTransaction(),
            receiveAddress: "queue@machine",
            context: new ContextBag()
        );

        Assert.That(context.ReceiveProperties, Is.SameAs(ReceiveProperties.Empty));
    }
}