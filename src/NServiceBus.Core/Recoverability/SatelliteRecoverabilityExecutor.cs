namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Transport;

    class SatelliteRecoverabilityExecutor
    {
        public SatelliteRecoverabilityExecutor(
            IServiceProvider serviceProvider,
            FaultMetadataExtractor faultMetadataExtractor,
            Func<ErrorContext, RecoverabilityAction> recoverabilityPolicy)
        {
            this.serviceProvider = serviceProvider;
            this.faultMetadataExtractor = faultMetadataExtractor;
            this.recoverabilityPolicy = recoverabilityPolicy;
        }

        public async Task<ErrorHandleResult> Invoke(
            ErrorContext errorContext,
            CancellationToken cancellationToken = default)
        {
            var recoverabilityAction = recoverabilityPolicy(errorContext);
            var metadata = faultMetadataExtractor.Extract(errorContext);

            var transportOperations = recoverabilityAction.GetTransportOperations(errorContext, metadata);

            var dispatcher = serviceProvider.GetRequiredService<IMessageDispatcher>();

            await dispatcher.Dispatch(new TransportOperations(transportOperations.ToArray()), errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            return recoverabilityAction.ErrorHandleResult;
        }

        readonly IServiceProvider serviceProvider;
        readonly FaultMetadataExtractor faultMetadataExtractor;
        readonly Func<ErrorContext, RecoverabilityAction> recoverabilityPolicy;
    }
}