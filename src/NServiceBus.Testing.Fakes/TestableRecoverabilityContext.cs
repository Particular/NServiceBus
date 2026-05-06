namespace NServiceBus.Testing;

using System;
using System.Collections.Generic;
using Pipeline;
using Transport;

/// <summary>
/// A testable implementation of <see cref="IRecoverabilityContext" />.
/// </summary>
public partial class TestableRecoverabilityContext : TestableBehaviorContext, IRecoverabilityContext, IRecoverabilityActionContext
{
    /// <summary>
    /// The message that failed processing.
    /// </summary>
    public IncomingMessage FailedMessage { get; set; } = new(Guid.NewGuid().ToString(), [], ReadOnlyMemory<byte>.Empty);

    /// <summary>
    /// The message ID of the failed message.
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The native message ID of the failed message.
    /// </summary>
    public string NativeMessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The message headers of the failed message.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = [];

    /// <summary>
    /// The message body of the failed message.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Properties received from the transport that will be propagated to outgoing dispatch operations.
    /// </summary>
    public ReceiveProperties ReceiveProperties { get; set; } = ReceiveProperties.Empty;

    /// <summary>
    /// The exception that caused processing to fail.
    /// </summary>
    public Exception Exception { get; set; } = new Exception();

    /// <summary>
    /// The receive address where this message failed.
    /// </summary>
    public string ReceiveAddress { get; set; } = "receive-queue";

    /// <summary>
    /// The number of times the message have been retried immediately but failed.
    /// </summary>
    public int ImmediateProcessingFailures { get; set; }

    /// <summary>
    /// Number of delayed deliveries performed so far.
    /// </summary>
    public int DelayedDeliveriesPerformed { get; set; }

    /// <summary>
    /// Metadata for this message.
    /// </summary>
    IReadOnlyDictionary<string, string> IRecoverabilityActionContext.Metadata => Metadata;

    /// <summary>
    /// The recoverability configuration for the endpoint.
    /// </summary>
    public RecoverabilityConfig RecoverabilityConfiguration { get; set; } = new RecoverabilityConfig(
        new ImmediateConfig(0),
        new DelayedConfig(0, TimeSpan.Zero),
        new FailedConfig("error", []));

    /// <summary>
    /// The recoverability action to take for this message.
    /// </summary>
    public RecoverabilityAction RecoverabilityAction { get; set; }

    /// <summary>
    /// Metadata for this message.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Locks the recoverability action for further changes.
    /// </summary>
    public IRecoverabilityActionContext PreventChanges()
    {
        IsLocked = true;
        return this;
    }

    /// <summary>
    /// True if the recoverability action was locked.
    /// </summary>
    public bool IsLocked { get; private set; } = false;
}