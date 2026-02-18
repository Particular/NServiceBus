namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;
using Extensibility;

/// <summary>
/// Allows the transport to pass relevant info to the pipeline.
/// </summary>
public partial class MessageContext : IExtendable
{
    /// <summary>
    /// Initializes the context.
    /// </summary>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="body">The message body.</param>
    /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
    /// <param name="receiveAddress">The receive address.</param>
    /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
    public MessageContext(string nativeMessageId, Dictionary<string, string> headers, ReadOnlyMemory<byte> body, TransportTransaction transportTransaction, string receiveAddress, ContextBag context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nativeMessageId);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(transportTransaction);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveAddress);
        ArgumentNullException.ThrowIfNull(context);

        Headers = headers;
        Body = body;
        NativeMessageId = nativeMessageId;
        Extensions = context;
        ReceiveAddress = receiveAddress;
        TransportTransaction = transportTransaction;

        if (context.TryGet<DispatchProperties>(out var dispatchProperties))
        {
            context.Remove<DispatchProperties>();
            // Hack hardcoded string for now
            context.Set("IncomingMessage.DispatchProperties", dispatchProperties);
        }

        context.GetOrCreate<IncomingPipelineMetricTags>();
    }

    /// <summary>
    /// The native message ID.
    /// </summary>
    public string NativeMessageId { get; }

    /// <summary>
    /// The message headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; }

    /// <summary>
    /// The message body.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }

    /// <summary>
    /// Transaction (along with connection if applicable) used to receive the message.
    /// </summary>
    public TransportTransaction TransportTransaction { get; }

    /// <summary>
    /// Transport address that received the message.
    /// </summary>
    public string ReceiveAddress { get; }

    /// <summary>
    /// A <see cref="ContextBag" /> which can be used to extend the current object.
    /// </summary>
    public ContextBag Extensions { get; }
}