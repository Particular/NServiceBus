namespace NServiceBus.Transport;

using System;
using System.Collections.Generic;
using Extensibility;
using Particular.Obsoletes;

/// <summary>
/// The context for messages that has failed processing.
/// </summary>
public class ErrorContext
{
    /// <summary>
    /// Initializes the error context.
    /// </summary>
    /// <param name="exception">The exception that caused the message processing failure.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <param name="body">The message body.</param>
    /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
    /// <param name="immediateProcessingFailures">Number of failed immediate processing attempts.</param>
    /// <param name="receiveAddress">The receive address.</param>
    /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
    public ErrorContext(Exception exception, Dictionary<string, string> headers, string nativeMessageId, ReadOnlyMemory<byte> body, TransportTransaction transportTransaction, int immediateProcessingFailures, string receiveAddress, ContextBag context)
        : this(exception, headers, nativeMessageId, body, ReceiveProperties.Empty, transportTransaction, immediateProcessingFailures, receiveAddress, context)
    {
    }

    /// <summary>
    /// Initializes the error context with receive properties.
    /// </summary>
    /// <param name="exception">The exception that caused the message processing failure.</param>
    /// <param name="headers">The message headers.</param>
    /// <param name="nativeMessageId">The native message ID.</param>
    /// <param name="body">The message body.</param>
    /// <param name="receiveProperties">Properties received from the transport that can be propagated to outgoing messages.</param>
    /// <param name="transportTransaction">Transaction (along with connection if applicable) used to receive the message.</param>
    /// <param name="immediateProcessingFailures">Number of failed immediate processing attempts.</param>
    /// <param name="receiveAddress">The receive address.</param>
    /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
    public ErrorContext(Exception exception, Dictionary<string, string> headers, string nativeMessageId, ReadOnlyMemory<byte> body, ReceiveProperties receiveProperties, TransportTransaction transportTransaction, int immediateProcessingFailures, string receiveAddress, ContextBag context)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(transportTransaction);
        ArgumentOutOfRangeException.ThrowIfNegative(immediateProcessingFailures);
        ArgumentException.ThrowIfNullOrWhiteSpace(receiveAddress);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(receiveProperties);

        Exception = exception;
        TransportTransaction = transportTransaction;
        ImmediateProcessingFailures = immediateProcessingFailures;
        NativeMessageId = nativeMessageId;
        MessageId = IncomingMessage.GetOrSetMessageIdFromHeaders(headers, nativeMessageId);
        Headers = headers;
        Body = body;
        ReceiveProperties = receiveProperties;

        ReceiveAddress = receiveAddress;

        DelayedDeliveriesPerformed = headers.GetDelayedDeliveriesPerformed();
        Extensions = context;
    }

    /// <summary>
    /// Exception that caused the message processing to fail.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Transport transaction for failed receive message.
    /// </summary>
    public TransportTransaction TransportTransaction { get; }

    /// <summary>
    /// Number of failed immediate processing attempts. This number is re-set with each delayed delivery.
    /// </summary>
    public int ImmediateProcessingFailures { get; }

    /// <summary>
    /// Number of delayed deliveries performed so far.
    /// </summary>
    public int DelayedDeliveriesPerformed { get; }

    /// <summary>
    /// The message ID.
    /// </summary>
    public string MessageId { get; }

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
    /// Properties received from the transport that can be propagated to outgoing dispatch operations.
    /// </summary>
    public ReceiveProperties ReceiveProperties { get; }

    /// <summary>
    /// Failed incoming message.
    /// </summary>
    [ObsoleteMetadata(Message = "For access to the message body, headers, native message ID, or the receive properties use the corresponding properties directly exposed on the context", TreatAsErrorFromVersion = "11", RemoveInVersion = "12")]
    [Obsolete("For access to the message body, headers, native message ID, or the receive properties use the corresponding properties directly exposed on the context. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public IncomingMessage Message => new(NativeMessageId, Headers, Body, ReceiveProperties);

    /// <summary>
    /// Transport address that received the failed message.
    /// </summary>
    public string ReceiveAddress { get; }

    /// <summary>
    /// A collection of additional information provided by the transport.
    /// </summary>
    public ContextBag Extensions { get; }
}