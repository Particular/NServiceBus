#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;
using Transport;

class SatelliteRecoverabilityExecutor<TState>(
    IServiceProvider serviceProvider,
    FaultMetadataExtractor faultMetadataExtractor,
    Func<ErrorContext, TState, RecoverabilityAction> recoverabilityPolicy,
    TState state)
    : IRecoverabilityPipelineExecutor
{
    public async Task<ErrorHandleResult> Invoke(
        ErrorContext errorContext,
        CancellationToken cancellationToken = default)
    {
        var recoverabilityAction = recoverabilityPolicy(errorContext, state);
        var metadata = faultMetadataExtractor.Extract(errorContext);

        var actionContext = new BehaviorActionContext(
            errorContext,
            metadata,
            serviceProvider,
            cancellationToken);

        List<TransportOperation>? transportOperations = null;
        var routingContexts = recoverabilityAction.GetRoutingContexts(actionContext);

        foreach (var routingContext in routingContexts)
        {
            // using the count here is not entirely accurate because of the way we duplicate based on the strategies
            // but in many cases it is a good approximation.
            transportOperations ??= new List<TransportOperation>(routingContexts.Count);
            // when there are more than one routing strategy we want to make sure each transport operation is independent
            var copySharedMutableMessageState = routingContext.RoutingStrategies.Count > 1;
            foreach (var strategy in routingContext.RoutingStrategies)
            {
                var transportOperation = routingContext.ToTransportOperation(strategy, DispatchConsistency.Default, copySharedMutableMessageState);
                transportOperations.Add(transportOperation);
            }
        }

        if (transportOperations == null)
        {
            return recoverabilityAction.ErrorHandleResult;
        }

        var dispatcher = serviceProvider.GetRequiredService<IMessageDispatcher>();
        await dispatcher.Dispatch(new TransportOperations([.. transportOperations]), errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

        return recoverabilityAction.ErrorHandleResult;
    }

    sealed class BehaviorActionContext(
        ErrorContext errorContext,
        IReadOnlyDictionary<string, string> metadata,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
        : IRecoverabilityActionContext
    {
        public IncomingMessage FailedMessage { get; } = errorContext.Message;

        public Exception Exception { get; } = errorContext.Exception;

        public string ReceiveAddress { get; } = errorContext.ReceiveAddress;

        public int ImmediateProcessingFailures { get; } = errorContext.ImmediateProcessingFailures;

        public int DelayedDeliveriesPerformed { get; } = errorContext.DelayedDeliveriesPerformed;

        public CancellationToken CancellationToken { get; } = cancellationToken;

        public ContextBag Extensions => field ??= new ContextBag();
        public IServiceProvider Builder { get; } = serviceProvider;
        public IReadOnlyDictionary<string, string> Metadata { get; } = metadata;
    }
}