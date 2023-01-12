namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityPipelineExecutor<TState> : IRecoverabilityPipelineExecutor
    {
        public RecoverabilityPipelineExecutor(
            IServiceProvider serviceProvider,
            IPipelineCache pipelineCache,
            MessageOperations messageOperations,
            RecoverabilityConfig recoverabilityConfig,
            Func<ErrorContext, TState, RecoverabilityAction> recoverabilityPolicy,
            IPipeline<IRecoverabilityContext> recoverabilityPipeline,
            FaultMetadataExtractor faultMetadataExtractor,
            TState state)
        {
            this.state = state;
            this.serviceProvider = serviceProvider;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.recoverabilityConfig = recoverabilityConfig;
            this.recoverabilityPolicy = recoverabilityPolicy;
            this.recoverabilityPipeline = recoverabilityPipeline;
            this.faultMetadataExtractor = faultMetadataExtractor;
        }

        public async Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            var childScope = serviceProvider.CreateAsyncScope();
            await using (childScope.ConfigureAwait(false))
            {
                var recoverabilityAction = recoverabilityPolicy(errorContext, state);

                var metadata = faultMetadataExtractor.Extract(errorContext);

                var recoverabilityContext = new RecoverabilityContext(
                    childScope.ServiceProvider,
                    messageOperations,
                    pipelineCache,
                    errorContext,
                    recoverabilityConfig,
                    metadata,
                    recoverabilityAction,
                    errorContext.Extensions,
                    cancellationToken);

                await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);

                return recoverabilityContext.RecoverabilityAction.ErrorHandleResult;
            }
        }

        readonly IServiceProvider serviceProvider;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly RecoverabilityConfig recoverabilityConfig;
        readonly Func<ErrorContext, TState, RecoverabilityAction> recoverabilityPolicy;
        readonly IPipeline<IRecoverabilityContext> recoverabilityPipeline;
        readonly FaultMetadataExtractor faultMetadataExtractor;
        readonly TState state;
    }
}