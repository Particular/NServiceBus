#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Collections.Generic;
using NServiceBus.Transport;
using Particular.Obsoletes;

/// <summary>
/// Provide context to behaviors on the recoverability pipeline.
/// </summary>
public interface IRecoverabilityContext : IBehaviorContext
{
    /// <summary>
    /// The message that failed processing.
    /// </summary>
    [ObsoleteMetadata(Message = "For access to the message body, headers, native message ID or the receive properties using the corresponding properties directly exposed on the context.", TreatAsErrorFromVersion = "11", RemoveInVersion = "12")]
    [Obsolete("For access to the message body, headers, native message ID or the receive properties using the corresponding properties directly exposed on the context.. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    IncomingMessage FailedMessage { get; }

#pragma warning disable CS0618 // Type or member is obsolete. once FailedMessage is removed simply turn these properties into get only properties.
    /// <summary>
    /// The message ID of the failed message.
    /// </summary>
    string MessageId => FailedMessage.MessageId;

    /// <summary>
    /// The native message ID of the failed message.
    /// </summary>
    string NativeMessageId => FailedMessage.NativeMessageId;

    /// <summary>
    /// The message headers of the failed message.
    /// </summary>
    Dictionary<string, string> Headers => FailedMessage.Headers;

    /// <summary>
    /// The message body of the failed message.
    /// </summary>
    ReadOnlyMemory<byte> Body => FailedMessage.Body;

    /// <summary>
    /// Properties received from the transport that can be propagated to outgoing dispatch operations.
    /// </summary>
    ReceiveProperties ReceiveProperties => FailedMessage.ReceiveProperties;
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// The exception that caused processing to fail.
    /// </summary>
    Exception Exception { get; }

    /// <summary>
    /// The receive address where this message failed.
    /// </summary>
    string ReceiveAddress { get; }

    /// <summary>
    /// The number of times the message have been retried immediately but failed.
    /// </summary>
    int ImmediateProcessingFailures { get; }

    /// <summary>
    /// Number of delayed deliveries performed so far.
    /// </summary>
    int DelayedDeliveriesPerformed { get; }

    /// <summary>
    /// The recoverability configuration for the endpoint.
    /// </summary>
    RecoverabilityConfig RecoverabilityConfiguration { get; }

    /// <summary>
    /// The recoverability action to take for this message.
    /// </summary>
    RecoverabilityAction RecoverabilityAction { get; set; }

    /// <summary>
    /// Metadata for this message.
    /// </summary>
    Dictionary<string, string> Metadata { get; }

    /// <summary>
    /// Locks the recoverability action for further changes.
    /// </summary>
    IRecoverabilityActionContext PreventChanges();
}