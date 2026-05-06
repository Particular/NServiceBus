#nullable enable

namespace NServiceBus;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Extensibility;
using NServiceBus.Transport;
using Particular.Obsoletes;
using Pipeline;

sealed class RecoverabilityContext : PipelineRootContext, IRecoverabilityContext, IRecoverabilityActionContext, IRecoverabilityActionContextNotifications
{
    public RecoverabilityContext(
        IServiceProvider serviceProvider,
        MessageOperations messageOperations,
        IPipelineCache pipelineCache,
        ErrorContext errorContext,
        RecoverabilityConfig recoverabilityConfig,
        Dictionary<string, string> metadata,
        RecoverabilityAction recoverabilityAction,
        ContextBag parent,
        CancellationToken cancellationToken) : base(serviceProvider, messageOperations, pipelineCache, cancellationToken, parent)
    {
#pragma warning disable CS0618 // Type or member is obsolete. Can be removed in the next major when FailedMessage is removed from the interface.
        FailedMessage = errorContext.Message;
#pragma warning restore CS0618 // Type or member is obsolete
        MessageId = errorContext.MessageId;
        NativeMessageId = errorContext.NativeMessageId;
        Headers = errorContext.Headers;
        Body = errorContext.Body;
        ReceiveProperties = errorContext.ReceiveProperties;
        Exception = errorContext.Exception;
        ReceiveAddress = errorContext.ReceiveAddress;
        ImmediateProcessingFailures = errorContext.ImmediateProcessingFailures;
        DelayedDeliveriesPerformed = errorContext.DelayedDeliveriesPerformed;
        RecoverabilityConfiguration = recoverabilityConfig;
        Metadata = metadata;
        RecoverabilityAction = recoverabilityAction;

        Extensions.Set(errorContext.TransportTransaction);
    }

    [ObsoleteMetadata(Message = "For access to the message body, headers, native message ID, or the receive properties use the corresponding properties directly exposed on the context", TreatAsErrorFromVersion = "11", RemoveInVersion = "12")]
    [Obsolete("For access to the message body, headers, native message ID, or the receive properties use the corresponding properties directly exposed on the context. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public IncomingMessage FailedMessage { get; } // Can be removed in the next major when FailedMessage is removed from the interface.

    public Exception Exception { get; }

    public string ReceiveAddress { get; }

    public int ImmediateProcessingFailures { get; }

    public int DelayedDeliveriesPerformed { get; }

    public string MessageId { get; }

    public string NativeMessageId { get; }

    public Dictionary<string, string> Headers { get; }

    public ReadOnlyMemory<byte> Body { get; }

    public ReceiveProperties ReceiveProperties { get; }

    IReadOnlyDictionary<string, string> IRecoverabilityActionContext.Metadata => Metadata;

    public RecoverabilityConfig RecoverabilityConfiguration { get; }

    public Dictionary<string, string> Metadata { get; }

    public RecoverabilityAction RecoverabilityAction
    {
        get;
        set
        {
            if (locked)
            {
                throw new InvalidOperationException(
                    "The RecoverabilityAction has already been executed and can't be changed");
            }

            field = value;
        }
    }

    public IRecoverabilityActionContext PreventChanges()
    {
        locked = true;
        return this;
    }

    public void Add(object notification)
    {
        notifications ??= [];
        notifications.Add(notification);
    }

    public IEnumerator<object> GetEnumerator() => notifications?.GetEnumerator() ?? Enumerable.Empty<object>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool locked;
    List<object>? notifications;
}