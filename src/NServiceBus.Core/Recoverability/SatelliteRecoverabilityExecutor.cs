﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Transport;
    using Pipeline;

    class SatelliteRecoverabilityExecutor
    {
        public SatelliteRecoverabilityExecutor(
            IServiceProvider serviceProvider,
            FaultMetadataExtractor faultMetadataExtractor,
            Func<ErrorContext, object, RecoverabilityAction> recoverabilityPolicy,
            object state)
        {
            this.state = state;
            this.serviceProvider = serviceProvider;
            this.faultMetadataExtractor = faultMetadataExtractor;
            this.recoverabilityPolicy = recoverabilityPolicy;
        }

        public async Task<ErrorHandleResult> Invoke(
            ErrorContext errorContext,
            CancellationToken cancellationToken = default)
        {
            var recoverabilityAction = recoverabilityPolicy(errorContext, state);
            var metadata = faultMetadataExtractor.Extract(errorContext);

            var actionContext = new BehaviorActionContext(errorContext, metadata, serviceProvider, cancellationToken);

            List<TransportOperation> transportOperations = null;
            var routingContexts = recoverabilityAction.GetRoutingContexts(actionContext);

            foreach (var routingContext in routingContexts)
            {
                // using the count here is not entirely accurate because of the way we duplicate based on the strategies
                // but in many cases it is a good approximation.
                transportOperations ??= new List<TransportOperation>(routingContexts.Count);
                foreach (var strategy in routingContext.RoutingStrategies)
                {
                    var transportOperation = routingContext.ToTransportOperation(strategy, DispatchConsistency.Default);
                    transportOperations.Add(transportOperation);
                }
            }

            if (transportOperations == null)
            {
                return recoverabilityAction.ErrorHandleResult;
            }

            var dispatcher = serviceProvider.GetRequiredService<IMessageDispatcher>();
            await dispatcher.Dispatch(new TransportOperations(transportOperations.ToArray()), errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            return recoverabilityAction.ErrorHandleResult;
        }

        class BehaviorActionContext : IRecoverabilityActionContext
        {
            public BehaviorActionContext(ErrorContext errorContext, IReadOnlyDictionary<string, string> metadata, IServiceProvider serviceProvider, CancellationToken cancellationToken)
            {
                ErrorContext = errorContext;
                Metadata = metadata;
                CancellationToken = cancellationToken;
                Builder = serviceProvider;
            }
            public CancellationToken CancellationToken { get; }

            public ContextBag Extensions => contextBag ??= new ContextBag();
            public IServiceProvider Builder { get; }
            public ErrorContext ErrorContext { get; }
            public IReadOnlyDictionary<string, string> Metadata { get; }

            ContextBag contextBag;
        }

        readonly IServiceProvider serviceProvider;
        readonly FaultMetadataExtractor faultMetadataExtractor;
        readonly Func<ErrorContext, object, RecoverabilityAction> recoverabilityPolicy;
        readonly object state;
    }
}