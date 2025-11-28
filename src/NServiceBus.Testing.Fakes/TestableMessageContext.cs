namespace NServiceBus.Testing;

using System;
using System.Collections.Generic;
using Extensibility;
using NServiceBus.Transport;

public class TestableMessageContext : MessageContext
{
    public TestableMessageContext(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, TransportTransaction transportTransaction, string receiveAddress, ContextBag context)
        : base(nativeMessageId, headers, body, transportTransaction, receiveAddress, context)
    {
    }

    public TestableMessageContext(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body)
        : base(nativeMessageId, headers, body, new TransportTransaction(), "receiveAddress", new ContextBag())
    {
    }
}