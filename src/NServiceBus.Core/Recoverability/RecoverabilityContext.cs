namespace NServiceBus;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Extensibility;
using NServiceBus.Transport;
using Pipeline;

class RecoverabilityContext : PipelineRootContext, IRecoverabilityContext, IRecoverabilityActionContext, IRecoverabilityActionContextNotifications
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
        FailedMessage = errorContext.Message;
        Exception = errorContext.Exception;
        ReceiveAddress = errorContext.ReceiveAddress;
        ImmediateProcessingFailures = errorContext.ImmediateProcessingFailures;
        DelayedDeliveriesPerformed = errorContext.DelayedDeliveriesPerformed;
        RecoverabilityConfiguration = recoverabilityConfig;
        Metadata = metadata;
        RecoverabilityAction = recoverabilityAction;

        Extensions.Set(errorContext.TransportTransaction);
    }

    public IncomingMessage FailedMessage { get; }

    public Exception Exception { get; }

    public string ReceiveAddress { get; }

    public int ImmediateProcessingFailures { get; }

    public int DelayedDeliveriesPerformed { get; }

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
    List<object> notifications;
}