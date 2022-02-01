namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Transport;

    class SatelliteRecoverabilityExecutor
    {
        public SatelliteRecoverabilityExecutor(
            IServiceProvider serviceProvider,
            Func<ErrorContext, RecoverabilityAction> recoverabilityPolicy)
        {
            this.serviceProvider = serviceProvider;
            this.recoverabilityPolicy = recoverabilityPolicy;
        }

        public async Task<ErrorHandleResult> Invoke(
            ErrorContext errorContext,
            CancellationToken cancellationToken = default)
        {
            var recoverabilityAction = recoverabilityPolicy(errorContext);

            var transportOperations = recoverabilityAction.Execute(errorContext, new Dictionary<string, string>());

            var dispatcher = serviceProvider.GetRequiredService<IMessageDispatcher>();

            await dispatcher.Dispatch(new TransportOperations(transportOperations.ToArray()), errorContext.TransportTransaction, cancellationToken).ConfigureAwait(false);

            return recoverabilityAction.ErrorHandleResult;
        }

        readonly IServiceProvider serviceProvider;
        readonly Func<ErrorContext, RecoverabilityAction> recoverabilityPolicy;
    }
}