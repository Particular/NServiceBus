namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityPipelineExecutor
    {
        public RecoverabilityPipelineExecutor(
            IServiceProvider serviceProvider,
            IPipelineCache pipelineCache,
            MessageOperations messageOperations,
            Pipeline<IRecoverabilityContext> recoverabilityPipeline)
        {
            this.serviceProvider = serviceProvider;
            this.pipelineCache = pipelineCache;
            this.messageOperations = messageOperations;
            this.recoverabilityPipeline = recoverabilityPipeline;
        }

        public async Task<ErrorHandleResult> Invoke(ErrorContext errorContext, CancellationToken cancellationToken = default)
        {
            using (var childScope = serviceProvider.CreateScope())
            {
                var rootContext = new RootContext(childScope.ServiceProvider, messageOperations, pipelineCache, cancellationToken);
                rootContext.Extensions.Merge(errorContext.Extensions);

                var recoverabilityContext = new RecoverabilityContext(errorContext, rootContext);

                await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);

                return recoverabilityContext.ActionToTake;
            }
        }

        readonly IServiceProvider serviceProvider;
        readonly IPipelineCache pipelineCache;
        readonly MessageOperations messageOperations;
        readonly Pipeline<IRecoverabilityContext> recoverabilityPipeline;
    }
}