#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Pipeline;
using Transport;

class RecoverabilityPipelineExecutor<TState>(
    IServiceProvider serviceProvider,
    HeaderPool headerPool,
    IPipelineCache pipelineCache,
    MessageOperations messageOperations,
    RecoverabilityConfig recoverabilityConfig,
    Func<ErrorContext, TState, RecoverabilityAction> recoverabilityPolicy,
    IPipeline<IRecoverabilityContext> recoverabilityPipeline,
    FaultMetadataExtractor faultMetadataExtractor,
    TState state)
    : IRecoverabilityPipelineExecutor
{
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
                headerPool,
                cancellationToken);

            await recoverabilityPipeline.Invoke(recoverabilityContext).ConfigureAwait(false);

            return recoverabilityContext.RecoverabilityAction.ErrorHandleResult;
        }
    }
}