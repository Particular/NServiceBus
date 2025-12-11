#nullable enable

namespace NServiceBus.Pipeline;

using System;
using System.Collections.Generic;
using NServiceBus.Transport;

/// <summary>
/// Provide context to behaviors on the recoverability pipeline.
/// </summary>
public interface IRecoverabilityContext : IBehaviorContext
{
    /// <summary>
    /// The message that failed processing.
    /// </summary>
    IncomingMessage FailedMessage { get; }

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